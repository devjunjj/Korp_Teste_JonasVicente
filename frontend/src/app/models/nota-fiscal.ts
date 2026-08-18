export enum StatusNotaFiscal {
  Aberta = 0,
  Fechada = 1
}

export interface NotaFiscalItem {
  id?: number;
  produtoId: number;
  quantidade: number;
}

export interface NotaFiscal {
  id: number;
  numero: number;
  status: StatusNotaFiscal;
  dataCriacao: string;
  itens: NotaFiscalItem[];
}

export interface NovaNotaFiscal {
  itens: { produtoId: number; quantidade: number }[];
}