import { Routes } from '@angular/router';
import { ProdutoLista } from './components/produto-lista/produto-lista';

export const routes: Routes = [
  { path: 'produtos', component: ProdutoLista },
  { path: '', redirectTo: 'produtos', pathMatch: 'full' }
];