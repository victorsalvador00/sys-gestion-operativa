import { registerLocaleData } from '@angular/common';
import localeEsMx from '@angular/common/locales/es-MX';
import {
  DEFAULT_CURRENCY_CODE,
  EnvironmentProviders,
  LOCALE_ID,
  makeEnvironmentProviders,
} from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { SpanishPaginatorIntl } from './spanish-paginator-intl';

export const APP_LOCALE = 'es-MX';

registerLocaleData(localeEsMx, APP_LOCALE);

/** Locale es-MX para pipes de Angular (moneda MXN) y textos de Angular Material. */
export function provideAppLocale(): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: LOCALE_ID, useValue: APP_LOCALE },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'MXN' },
    { provide: MatPaginatorIntl, useClass: SpanishPaginatorIntl },
  ]);
}
