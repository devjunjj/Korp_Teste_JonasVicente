import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProdutoService } from '../../services/produto.service';
import { Produto } from '../../models/produto';

@Component({
  selector: 'app-produto-lista',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './produto-lista.html',
  styleUrl: './produto-lista.scss'
})
export class ProdutoLista implements OnInit {
  produtos: Produto[] = [];
  carregando = true;
  colunasExibidas: string[] = ['codigo', 'descricao', 'saldo'];

  constructor(
    private produtoService: ProdutoService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.carregarProdutos();
  }

  carregarProdutos(): void {
    this.carregando = true;
    this.produtoService.listar().subscribe({
      next: (produtos) => {
        this.produtos = produtos;
        this.carregando = false;
        this.cdr.detectChanges();
      },
      error: (erro) => {
        console.error('Erro ao carregar produtos:', erro);
        this.carregando = false;
        this.cdr.detectChanges();
      }
    });
  }
}