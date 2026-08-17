import { Component, OnInit } from '@angular/core';
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

  constructor(private produtoService: ProdutoService) { }

  ngOnInit(): void {
    this.carregarProdutos();
  }

  carregarProdutos(): void {
    console.log('1. Iniciando busca de produtos...');
    this.carregando = true;
    this.produtoService.listar().subscribe({
      next: (produtos) => {
        console.log('2. Dados recebidos:', produtos);
        this.produtos = produtos;
        this.carregando = false;
        console.log('3. carregando agora é:', this.carregando);
      },
      error: (erro) => {
        console.error('ERRO ao carregar produtos:', erro);
        this.carregando = false;
      }
    });
  }
}