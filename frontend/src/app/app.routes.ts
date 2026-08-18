import { Routes } from '@angular/router';
import { ProdutoLista } from './components/produto-lista/produto-lista';
import { ProdutoForm } from './components/produto-form/produto-form';
import { NotaFiscalLista } from './components/nota-fiscal-lista/nota-fiscal-lista';
import { NotaFiscalForm } from './components/nota-fiscal-form/nota-fiscal-form';

export const routes: Routes = [
  { path: 'produtos/novo', component: ProdutoForm },
  { path: 'produtos', component: ProdutoLista },
  { path: 'notas-fiscais/nova', component: NotaFiscalForm },
  { path: 'notas-fiscais', component: NotaFiscalLista },
  { path: '', redirectTo: 'produtos', pathMatch: 'full' }
];