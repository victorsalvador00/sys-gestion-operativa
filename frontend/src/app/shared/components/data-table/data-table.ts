import { BreakpointObserver } from '@angular/cdk/layout';
import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChild,
  contentChildren,
  DestroyRef,
  Directive,
  inject,
  input,
  output,
  signal,
  TemplateRef,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorIntl, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import { ListQuery } from '../../../core/http/list-query';
import { SpanishPaginatorIntl } from '../../../core/i18n/spanish-paginator-intl';

export interface TableColumn<T> {
  /** Campo del backend: se usa para `sort` y como identificador de columna. */
  key: string;
  header: string;
  sortable?: boolean;
  align?: 'start' | 'end';
  /** Texto de la celda cuando no hay plantilla `appCell`. */
  value?: (row: T) => string | number | null | undefined;
}

/** Plantilla de celda: `<ng-template appCell="status" let-row>...</ng-template>`. */
@Directive({ selector: 'ng-template[appCell]' })
export class CellDef {
  readonly appCell = input.required<string>();
  readonly template = inject(TemplateRef<{ $implicit: unknown }>);
}

/**
 * Detalle expandible de una fila: `<ng-template appRowDetail let-row>...</ng-template>`. Con él, la tabla
 * agrega un botón para expandir cada fila (y el clic en la fila la expande si no es `rowClickable`).
 */
@Directive({ selector: 'ng-template[appRowDetail]' })
export class RowDetailDef {
  readonly template = inject(TemplateRef<{ $implicit: unknown }>);
}

/** Tarjeta por fila en celular: `<ng-template appCardDef let-row>...</ng-template>`. */
@Directive({ selector: 'ng-template[appCardDef]' })
export class CardDef {
  readonly template = inject(TemplateRef<{ $implicit: unknown }>);
}

export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];
const EXPAND_COLUMN = '__expand';

/**
 * Tabla de listados con paginación, orden y búsqueda del lado del servidor (spec frontend §8).
 * La página guarda la `query` y hace la petición; la tabla solo emite `queryChange`.
 */
@Component({
  selector: 'app-data-table',
  imports: [
    NgTemplateOutlet,
    ReactiveFormsModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
  ],
  providers: [{ provide: MatPaginatorIntl, useClass: SpanishPaginatorIntl }],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss',
})
export class DataTable<T> {
  readonly columns = input.required<TableColumn<T>[]>();
  readonly rows = input.required<T[]>();
  readonly total = input(0);
  readonly loading = input(false);
  readonly query = input.required<ListQuery>();
  readonly searchable = input(false);
  readonly searchPlaceholder = input('Buscar');
  readonly emptyMessage = input('No hay registros.');
  readonly emptyActionLabel = input<string>();
  readonly rowClickable = input(false);
  readonly trackBy = input<(row: T) => unknown>((row: T) => (row as { id?: unknown }).id ?? row);

  readonly queryChange = output<ListQuery>();
  readonly rowClick = output<T>();
  readonly emptyAction = output<void>();

  private readonly cellDefs = contentChildren(CellDef);
  protected readonly cardDef = contentChild(CardDef);
  protected readonly detailDef = contentChild(RowDetailDef);
  private readonly expanded = signal<ReadonlySet<unknown>>(new Set());

  protected readonly isMobile = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 767.98px)')
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );
  protected readonly showCards = computed(() => this.isMobile() && !!this.cardDef());

  protected readonly columnKeys = computed(() => [
    ...(this.detailDef() ? [EXPAND_COLUMN] : []),
    ...this.columns().map((c) => c.key),
  ]);
  protected readonly expandColumn = EXPAND_COLUMN;
  protected readonly templates = computed(
    () => new Map(this.cellDefs().map((def) => [def.appCell(), def.template])),
  );
  protected readonly sortActive = computed(() => this.query().sort?.split(':')[0] ?? '');
  protected readonly sortDirection = computed(() =>
    this.query().sort?.endsWith(':desc') ? 'desc' : this.query().sort ? 'asc' : '',
  );
  protected readonly skeletonRows = Array.from({ length: 5 }, (_, i) => i);
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;

  protected readonly search = new FormControl('', { nonNullable: true });

  constructor() {
    const subscription = this.search.valueChanges
      .pipe(
        debounceTime(300),
        map((text) => text.trim()),
        distinctUntilChanged(),
      )
      .subscribe((q) => this.queryChange.emit({ ...this.query(), q: q || undefined, page: 1 }));
    inject(DestroyRef).onDestroy(() => subscription.unsubscribe());
  }

  protected onPage(event: PageEvent): void {
    this.queryChange.emit({ ...this.query(), page: event.pageIndex + 1, pageSize: event.pageSize });
  }

  protected onSort(sort: Sort): void {
    this.queryChange.emit({
      ...this.query(),
      sort: sort.direction ? `${sort.active}:${sort.direction}` : undefined,
      page: 1,
    });
  }

  protected cellText(column: TableColumn<T>, row: T): string {
    const value = column.value ? column.value(row) : (row as Record<string, unknown>)[column.key];
    return value === null || value === undefined ? '' : String(value);
  }

  protected onRowClick(row: T): void {
    if (this.rowClickable()) {
      this.rowClick.emit(row);
    } else if (this.detailDef()) {
      this.toggle(row);
    }
  }

  protected isExpanded(row: T): boolean {
    return this.expanded().has(this.trackBy()(row));
  }

  protected toggle(row: T, event?: Event): void {
    event?.stopPropagation();
    const key = this.trackBy()(row);
    this.expanded.update((current) => {
      const next = new Set(current);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }
}
