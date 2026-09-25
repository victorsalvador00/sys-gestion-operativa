import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import type { ApiEnum, Schemas } from '../../core/api/api-types';
import { toHttpParams } from '../../core/http/list-query';

export type ItemOption = Schemas['ItemListItemDto'];
export type ItemType = ApiEnum<'ItemType'>;

/** Consultas ligeras de artículos para los selectores (no es el CRUD del catálogo). */
@Injectable({ providedIn: 'root' })
export class ItemLookupService {
  private readonly http = inject(HttpClient);

  search(q: string, type?: ItemType, pageSize = 20): Observable<ItemOption[]> {
    return this.http
      .get<Schemas['PagedResultOfItemListItemDto']>('/items', {
        params: toHttpParams({ q, pageSize, page: 1 }, { type }),
      })
      .pipe(map((result) => result.items));
  }

  /** Existencia por artículo en una ubicación, para los artículos que coinciden con `q`. */
  stock(locationId: string, q: string, pageSize = 20): Observable<Map<string, number>> {
    return this.http
      .get<Schemas['PagedResultOfStockLevelDto']>('/stock', {
        params: toHttpParams({ q, pageSize, page: 1 }, { locationId }),
      })
      .pipe(map((result) => new Map(result.items.map((level) => [level.itemId, level.onHand]))));
  }
}
