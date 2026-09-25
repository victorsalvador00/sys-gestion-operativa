import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ListQuery } from '../../../core/http/list-query';
import { toHttpParams } from '../../../core/http/list-query';
import { CellDef, DataTable, TableColumn } from './data-table';

interface Row {
  id: number;
  folio: string;
  total: number;
}

@Component({
  imports: [DataTable, CellDef],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-data-table
      [columns]="columns"
      [rows]="rows()"
      [total]="total()"
      [loading]="loading()"
      [query]="query()"
      [searchable]="true"
      emptyMessage="No hay órdenes."
      (queryChange)="changes.push($event); query.set($event)"
    >
      <ng-template appCell="total" let-row>$ {{ row.total }}</ng-template>
    </app-data-table>
  `,
})
class Host {
  readonly columns: TableColumn<Row>[] = [
    { key: 'folio', header: 'Folio', sortable: true },
    { key: 'total', header: 'Total', align: 'end' },
  ];
  readonly rows = signal<Row[]>([
    { id: 1, folio: 'OC-1', total: 10 },
    { id: 2, folio: 'OC-2', total: 20 },
  ]);
  readonly total = signal(60);
  readonly loading = signal(false);
  readonly query = signal<ListQuery>({ page: 1, pageSize: 25, sort: 'folio:asc' });
  readonly changes: ListQuery[] = [];
}

describe('app-data-table', () => {
  async function render() {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    return { fixture, host: fixture.componentInstance, el: fixture.nativeElement as HTMLElement };
  }

  it('muestra filas, celdas con plantilla y encabezados', async () => {
    const { el } = await render();
    expect(el.querySelectorAll('tr.mat-mdc-row')).toHaveLength(2);
    expect(el.querySelector('tr.mat-mdc-row td:last-child')?.textContent?.trim()).toBe('$ 10');
    expect(el.querySelector('th')?.textContent).toContain('Folio');
  });

  it('cambiar de página emite page/pageSize', async () => {
    const { el, host } = await render();
    el.querySelector<HTMLButtonElement>('button.mat-mdc-paginator-navigation-next')!.click();
    expect(host.changes.at(-1)).toEqual({ page: 2, pageSize: 25, sort: 'folio:asc' });
  });

  it('ordenar emite sort=campo:dirección y vuelve a la página 1', async () => {
    const { el, host, fixture } = await render();
    host.query.set({ page: 3, pageSize: 25, sort: 'folio:asc' });
    await fixture.whenStable();

    el.querySelector<HTMLElement>('th.mat-sort-header')!.click();
    expect(host.changes.at(-1)).toEqual({ page: 1, pageSize: 25, sort: 'folio:desc' });
  });

  it('buscar emite q con retraso y vuelve a la página 1', async () => {
    const { el, host } = await render();
    vi.useFakeTimers();
    try {
      const input = el.querySelector<HTMLInputElement>('input[type=search]')!;
      input.value = ' oc-7 ';
      input.dispatchEvent(new Event('input'));
      expect(host.changes).toHaveLength(0);

      vi.advanceTimersByTime(300);
      expect(host.changes.at(-1)).toEqual({ page: 1, pageSize: 25, sort: 'folio:asc', q: 'oc-7' });
    } finally {
      vi.useRealTimers();
    }
  });

  it('sin filas muestra el estado vacío; cargando muestra el esqueleto', async () => {
    const { fixture, host, el } = await render();
    host.rows.set([]);
    host.total.set(0);
    await fixture.whenStable();
    expect(el.querySelector('.empty')?.textContent).toContain('No hay órdenes.');

    host.loading.set(true);
    await fixture.whenStable();
    expect(el.querySelector('.empty')).toBeNull();
    expect(el.querySelectorAll('.skeleton').length).toBeGreaterThan(0);
  });
});

describe('toHttpParams', () => {
  it('omite vacíos y agrega filtros', () => {
    const params = toHttpParams(
      { page: 2, pageSize: 25, sort: 'folio:desc', q: '' },
      { status: 'Approved', locationId: null },
    );
    expect(params.toString()).toBe('page=2&pageSize=25&sort=folio:desc&status=Approved');
  });
});
