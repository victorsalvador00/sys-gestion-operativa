import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { enumLabel, EnumName } from '../../pipes/status-label.pipe';

export type StatusColor = 'gray' | 'blue' | 'yellow' | 'green' | 'orange' | 'red';

/** Colores de estado de la spec frontend §4 (mismo color para el mismo estado en cualquier documento). */
const STATUS_COLORS: Record<string, StatusColor> = {
  Draft: 'gray',
  Submitted: 'blue',
  Released: 'blue',
  InProgress: 'blue',
  PendingApproval: 'yellow',
  Dispatched: 'yellow',
  PartiallyReceived: 'yellow',
  PartiallyFulfilled: 'yellow',
  Approved: 'green',
  Received: 'green',
  Completed: 'green',
  Fulfilled: 'green',
  Closed: 'green',
  Posted: 'green',
  Converted: 'green',
  ReceivedWithDiscrepancies: 'orange',
  Cancelled: 'red',
  Rejected: 'red',
};

export function statusColor(status: string): StatusColor {
  return STATUS_COLORS[status] ?? 'gray';
}

/**
 * `<app-status-tag status="PendingApproval" kind="PurchaseOrderStatus" />` → etiqueta amarilla "Por aprobar".
 * Para estados que no son enums del backend: `<app-status-tag label="Bloqueado" color="red" />`.
 */
@Component({
  selector: 'app-status-tag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': '"tag tag-" + resolvedColor()' },
  template: '{{ text() }}',
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      padding: 2px 10px;
      border-radius: 999px;
      font: var(--mat-sys-label-medium);
      white-space: nowrap;
    }
    :host(.tag-gray) {
      background: var(--sgo-status-gray-bg);
      color: var(--sgo-status-gray-fg);
    }
    :host(.tag-blue) {
      background: var(--sgo-status-blue-bg);
      color: var(--sgo-status-blue-fg);
    }
    :host(.tag-yellow) {
      background: var(--sgo-status-yellow-bg);
      color: var(--sgo-status-yellow-fg);
    }
    :host(.tag-green) {
      background: var(--sgo-status-green-bg);
      color: var(--sgo-status-green-fg);
    }
    :host(.tag-orange) {
      background: var(--sgo-status-orange-bg);
      color: var(--sgo-status-orange-fg);
    }
    :host(.tag-red) {
      background: var(--sgo-status-red-bg);
      color: var(--sgo-status-red-fg);
    }
  `,
})
export class StatusTag {
  readonly status = input('');
  readonly kind = input<EnumName>();
  /** Texto y color explícitos (tienen prioridad sobre `status`/`kind`). */
  readonly label = input<string>();
  readonly color = input<StatusColor>();

  protected readonly resolvedColor = computed(() => this.color() ?? statusColor(this.status()));
  protected readonly text = computed(() => {
    const kind = this.kind();
    return this.label() ?? (kind ? enumLabel(kind, this.status()) : this.status());
  });
}
