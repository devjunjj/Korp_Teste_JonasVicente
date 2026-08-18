import { Routes } from '@angular/router';
import { ProdutoLista } from './components/produto-lista/produto-lista';
import { ProdutoForm } from './components/produto-form/produto-form';

export const routes: Routes = [
  { path: 'produtos/novo', component: ProdutoForm },
  { path: 'produtos', component: ProdutoLista },
  { path: '', redirectTo: 'produtos', pathMatch: 'full' }
];