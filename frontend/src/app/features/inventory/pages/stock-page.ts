import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { LocationContextService } from '../../../core/context/location-context.service';
import { defaultListQuery, ListQuery } from '../../../core/http/list-query';
import {
  CardDef,
  CellDef,
  DataTable,
  RowDetailDef,
  TableColumn,
} from '../../../shared/components/data-table/data-table';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { StatusTag } from '../../../shared/components/status-tag/status-tag';
import { MxnPipe } from '../../../shared/pipes/mxn.pipe';
import { QtyPipe } from '../../../shared/pipes/qty.pipe';
import { CategoriesApi } from '../../catalog/data-access/categories.api';
import { StockApi, StockFilters, StockLevel } from '../data-access/stock.api';
import { LotList } from '../ui/lot-list';

export function minMaxText(level: StockLevel): string {
  return level.minQty === null || level.maxQty === null
    ? '—'
    : `${level.minQty} / ${level.maxQty} ${level.baseUomCode}`;
}

/** Existencias de la ubicación activa (spec frontend §7.3). */
@Component({
  selector: 'app-stock-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTooltipModule,
    PageHeader,
    DataTable,
    CellDef,
    CardDef,
    RowDetailDef,
    StatusTag,
    QtyPipe,
    MxnPipe,
    LotList,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-page.html',
  styles: `
    .filters {
      margin-bottom: var(--sgo-space-4);
    }
    .filters mat-form-field {
      width: 220px;
    }
    .summary {
      margin: 0 0 var(--sgo-space-3);
      color: var(--mat-sys-on-surface-variant);
    }
    .card-row {
      display: flex;
      justify-content: space-between;
      gap: var(--sgo-space-2);
    }
    .muted {
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class StockPage {
  private readonly api = inject(StockApi);
  private readonly categoriesApi = inject(CategoriesApi);
  protected readonly location = inject(LocationContextService).activeLocation;

  protected readonly columns: TableColumn<StockLevel>[] = [
    { key: 'sku', header: 'SKU', sortable: true },
    { key: 'itemName', header: 'Artículo', sortable: true },
    { key: 'onHand', header: 'Existencia', sortable: true, align: 'end' },
    { key: 'averageCost', header: 'Costo promedio', align: 'end' },
    { key: 'stockValue', header: 'Valor', align: 'end' },
    { key: 'minMax', header: 'Mín / Máx', value: minMaxText },
    { key: 'status', header: 'Estado' },
    { key: 'actions', header: '' },
  ];

  protected readonly query = signal<ListQuery>(defaultListQuery('sku:asc'));
  protected readonly filters = signal<StockFilters>({});

  protected readonly stock = rxResource({
    params: () => {
      const locationId = this.location()?.id;
      return locationId
        ? { query: this.query(), filters: { ...this.filters(), locationId } }
        : undefined;
    },
    stream: ({ params }) => this.api.list(params.query, params.filters),
  });

  /** Lotes por caducar según los días de alerta configurados (RN-07). */
  private readonly alerts = rxResource({
    params: () => this.location()?.id,
    stream: ({ params }) => this.api.alerts(params),
  });
  protected readonly expiringLotIds = computed(
    () => new Set((this.alerts.value()?.expiringLots ?? []).map((lot) => lot.lotId)),
  );
  protected readonly alertDays = computed(() => this.alerts.value()?.expirationAlertDays ?? null);

  protected readonly categories = rxResource({
    stream: () =>
      this.categoriesApi.list({ page: 1, pageSize: 100 }).pipe(map((page) => page.items)),
  });

  protected setFilter<K extends keyof StockFilters>(key: K, value: StockFilters[K]): void {
    this.filters.update((filters) => ({ ...filters, [key]: value }));
    this.query.update((query) => ({ ...query, page: 1 }));
  }

  /** Existencia por (ubicación, artículo): el id de artículo identifica la fila en una ubicación. */
  protected readonly trackItem = (row: StockLevel) => `${row.locationId}|${row.itemId}`;
}
