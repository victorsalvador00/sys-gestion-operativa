import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { ImportRowError, toProblem } from '../../../core/http/problem-details';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CsvTable, downloadText, parseCsv, toCsv } from '../../../shared/data-access/csv';
import { ItemImportResult, ItemsApi } from '../data-access/items.api';
import {
  ITEM_IMPORT_COLUMNS,
  ITEM_IMPORT_EXAMPLE_ROWS,
  ITEM_IMPORT_GUIDE,
  ITEM_IMPORT_REQUIRED,
} from '../ui/item-import-template';

/** Tamaño máximo que acepta el backend (`ImportsController.MaxFileBytes`). */
export const MAX_IMPORT_BYTES = 2 * 1024 * 1024;
export const PREVIEW_ROWS = 20;

/** Revisión rápida del encabezado en el navegador (el backend valida todo al importar). */
export function headerProblems(headers: string[]): string[] {
  const problems: string[] = [];
  const missing = ITEM_IMPORT_REQUIRED.filter((column) => !headers.includes(column));
  const unknown = headers.filter((h) => !(ITEM_IMPORT_COLUMNS as readonly string[]).includes(h));
  if (missing.length) {
    problems.push(`Faltan columnas obligatorias: ${missing.join(', ')}.`);
  }
  if (unknown.length) {
    problems.push(`Columnas desconocidas: ${unknown.join(', ')}.`);
  }
  return problems;
}

/** Importación de artículos por CSV (spec frontend §7.2): plantilla, vista previa y errores por fila. */
@Component({
  selector: 'app-item-import-page',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, PageHeader],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './item-import-page.html',
  styleUrl: './item-import-page.scss',
})
export class ItemImportPage {
  private readonly api = inject(ItemsApi);

  protected readonly guide = ITEM_IMPORT_GUIDE;
  protected readonly file = signal<File | null>(null);
  protected readonly table = signal<CsvTable | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly importing = signal(false);
  protected readonly result = signal<ItemImportResult | null>(null);
  protected readonly rowErrors = signal<ImportRowError[]>([]);
  protected readonly importError = signal<string | null>(null);

  protected readonly preview = computed(() => this.table()?.rows.slice(0, PREVIEW_ROWS) ?? []);
  protected readonly totalRows = computed(() => this.table()?.rows.length ?? 0);
  protected readonly headerWarnings = computed(() => {
    const table = this.table();
    return table ? headerProblems(table.headers) : [];
  });
  protected readonly errorLines = computed(() => new Set(this.rowErrors().map((e) => e.row)));

  protected downloadTemplate(): void {
    downloadText(
      'plantilla-articulos.csv',
      toCsv([...ITEM_IMPORT_COLUMNS], ITEM_IMPORT_EXAMPLE_ROWS),
    );
  }

  protected async onFile(input: HTMLInputElement): Promise<void> {
    const file = input.files?.[0] ?? null;
    input.value = '';
    this.reset();
    if (!file) {
      return;
    }
    if (file.size > MAX_IMPORT_BYTES) {
      this.fileError.set('El archivo pesa más de 2 MB. Divídelo en varios archivos.');
      return;
    }
    const table = parseCsv(await file.text());
    if (!table.headers.length || !table.rows.length) {
      this.fileError.set('El archivo está vacío o no tiene filas de datos.');
      return;
    }
    this.file.set(file);
    this.table.set(table);
  }

  protected import(): void {
    const file = this.file();
    if (!file || this.importing()) {
      return;
    }
    this.importing.set(true);
    this.rowErrors.set([]);
    this.importError.set(null);
    this.api.import(file).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.result.set(result);
      },
      error: (error: unknown) => {
        this.importing.set(false);
        const problem = toProblem(error);
        if (problem?.rowErrors?.length) {
          this.rowErrors.set(problem.rowErrors);
        } else if (problem?.status === 400) {
          this.importError.set(problem.detail ?? 'El archivo no es válido.');
        }
      },
    });
  }

  protected reset(): void {
    this.file.set(null);
    this.table.set(null);
    this.fileError.set(null);
    this.result.set(null);
    this.rowErrors.set([]);
    this.importError.set(null);
  }
}
