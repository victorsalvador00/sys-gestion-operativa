using Sgo.Domain.Common;
using Sgo.Domain.Inventory;

namespace Sgo.Domain.Purchasing;

public enum PurchaseOrderStatus
{
    Draft,
    PendingApproval,
    Approved,
    PartiallyReceived,
    Received,
    Cancelled,
    Closed,
}

/// <param name="Quantity">In the item's purchase unit.</param>
/// <param name="UnitPrice">Per purchase unit, MXN without VAT (RN-30: suggested from SupplierItem.Price).</param>
/// <param name="TaxRate">VAT as a fraction, e.g. 0.16.</param>
public sealed record PurchaseOrderLineInput(Guid ItemId, decimal Quantity, decimal UnitPrice, decimal TaxRate, Guid? RequisitionLineId);

public static class Money
{
    /// <summary>Document amounts are stored with 2 decimals.</summary>
    public static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Purchase order (dominio §4.6): Draft → PendingApproval → Approved → PartiallyReceived → Received; Cancelled | Closed.
/// B-13 creates drafts from requisitions; editing, approval (RN-31) and receipts (RN-32/33) arrive with B-14.
/// </summary>
[Audited]
public class PurchaseOrder : AuditableEntity, IVersioned
{
    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder() { }

    public PurchaseOrder(string folio, Guid supplierId, Guid deliveryLocationId, DateOnly? expectedDate, string? notes,
        IReadOnlyList<PurchaseOrderLineInput> lines)
    {
        Folio = folio;
        SupplierId = supplierId;
        DeliveryLocationId = deliveryLocationId;
        Status = PurchaseOrderStatus.Draft;
        SetContent(expectedDate, notes, lines);
    }

    public string Folio { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public Guid DeliveryLocationId { get; private set; }
    public DateOnly? ExpectedDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Without VAT.</summary>
    public decimal Subtotal { get; private set; }

    public decimal TaxTotal { get; private set; }
    public decimal Total { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public uint Version { get; private set; }
    public IReadOnlyList<PurchaseOrderLine> Lines => _lines;

    private void SetContent(DateOnly? expectedDate, string? notes, IReadOnlyList<PurchaseOrderLineInput> lines)
    {
        if (lines.Count == 0)
            throw new BusinessRuleException("purchase_order_without_lines", "La orden de compra debe tener al menos una línea.");
        if (lines.Any(l => l.Quantity <= 0 || InventoryMath.Round(l.Quantity) != l.Quantity))
            throw new BusinessRuleException("invalid_quantity", "Las cantidades deben ser mayores que cero, con máximo 4 decimales.");
        if (lines.Any(l => l.UnitPrice < 0 || l.TaxRate < 0))
            throw new BusinessRuleException("purchase_order_invalid_price", "El precio y el IVA no pueden ser negativos.");

        ExpectedDate = expectedDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        _lines.Clear();
        _lines.AddRange(lines.Select(l => new PurchaseOrderLine(Id, l.ItemId, l.Quantity, l.UnitPrice, l.TaxRate, l.RequisitionLineId)));
        Subtotal = _lines.Sum(l => l.Subtotal);
        TaxTotal = _lines.Sum(l => l.TaxAmount);
        Total = Subtotal + TaxTotal;
    }
}

public class PurchaseOrderLine : Entity
{
    private PurchaseOrderLine() { }

    internal PurchaseOrderLine(Guid purchaseOrderId, Guid itemId, decimal quantity, decimal unitPrice, decimal taxRate, Guid? requisitionLineId)
    {
        PurchaseOrderId = purchaseOrderId;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        RequisitionLineId = requisitionLineId;
    }

    public Guid PurchaseOrderId { get; private set; }
    public Guid ItemId { get; private set; }

    /// <summary>In the item's purchase unit.</summary>
    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; }

    /// <summary>In the item's purchase unit.</summary>
    public decimal ReceivedQty { get; private set; }

    public Guid? RequisitionLineId { get; private set; }

    public decimal Subtotal => Money.Round(Quantity * UnitPrice);
    public decimal TaxAmount => Money.Round(Subtotal * TaxRate);
}

/// <summary>A purchase order to create from approved requisitions (RN-34).</summary>
public sealed record PurchaseOrderPlan(
    Guid SupplierId, Guid DeliveryLocationId, DateOnly ExpectedDate, IReadOnlyList<string> RequisitionFolios,
    IReadOnlyList<PurchaseOrderLineInput> Lines);

public static class RequisitionConversion
{
    /// <summary>
    /// RN-34: groups the lines of approved requisitions by suggested supplier and delivery location (one PO has a
    /// single delivery location). One PO line per requisition line, keeping the link; the expected date is the
    /// earliest NeededBy of the group. <paramref name="priceOf"/> gives the supplier's price for an item.
    /// </summary>
    public static IReadOnlyList<PurchaseOrderPlan> Plan(
        IReadOnlyList<PurchaseRequisition> requisitions,
        Func<Guid, Guid, decimal> priceOf,
        Func<Guid, decimal> taxRateOf)
    {
        if (requisitions.Count == 0)
            throw new BusinessRuleException("requisition_conversion_empty", "Selecciona al menos una requisición.");
        if (requisitions.FirstOrDefault(r => r.Status != RequisitionStatus.Approved) is { } notApproved)
            throw new BusinessRuleException("requisition_not_approved",
                $"La requisición {notApproved.Folio} no está aprobada; solo las aprobadas se convierten en OC.");

        return requisitions
            .SelectMany(r => r.Lines.Select(line => (Requisition: r, Line: line)))
            .GroupBy(x => (SupplierId: x.Line.SuggestedSupplierId
                                       ?? throw new InvalidOperationException("Approved requisition line without supplier."),
                           x.Requisition.LocationId))
            .OrderBy(g => g.Key.SupplierId).ThenBy(g => g.Key.LocationId)
            .Select(g => new PurchaseOrderPlan(
                g.Key.SupplierId,
                g.Key.LocationId,
                g.Min(x => x.Requisition.NeededBy),
                g.Select(x => x.Requisition.Folio).Distinct().Order().ToList(),
                g.Select(x => new PurchaseOrderLineInput(x.Line.ItemId, x.Line.Quantity, priceOf(g.Key.SupplierId, x.Line.ItemId),
                    taxRateOf(x.Line.ItemId), x.Line.Id)).ToList()))
            .ToList();
    }
}
