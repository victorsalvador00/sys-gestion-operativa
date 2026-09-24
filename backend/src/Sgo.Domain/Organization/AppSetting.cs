using Sgo.Domain.Common;

namespace Sgo.Domain.Organization;

/// <summary>Key/value system parameter (dominio §4.7).</summary>
[Audited]
public class AppSetting : IVersioned
{
    private AppSetting() { }

    public AppSetting(string key, string value, string description)
    {
        Key = key;
        Value = value;
        Description = description;
    }

    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public uint Version { get; private set; }

    public void SetValue(string value) => Value = value;
}

public static class AppSettingKeys
{
    /// <summary>RN-31: PO total (MXN, before tax) at or above which approval is required.</summary>
    public const string PoApprovalThreshold = "purchasing.po_approval_threshold";

    /// <summary>RN-32: allowed over-receipt percentage.</summary>
    public const string ReceiptTolerancePct = "purchasing.receipt_tolerance_pct";

    /// <summary>RN-07: days ahead to warn about expiring lots.</summary>
    public const string ExpirationAlertDays = "inventory.expiration_alert_days";

    public static readonly IReadOnlyList<AppSetting> Defaults =
    [
        // Decision pending (dominio §7.3): 0 means every PO requires approval until configured.
        new(PoApprovalThreshold, "0", "Monto (MXN sin IVA) a partir del cual una OC requiere aprobación"),
        new(ReceiptTolerancePct, "0", "Porcentaje permitido de sobre-recepción en compras"),
        new(ExpirationAlertDays, "3", "Días de anticipación para la alerta de lotes por caducar"),
    ];
}
