import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { NotaFiscal, StatusNotaFiscal } from '../../models/nota-fiscal';

@Component({
  selector: 'app-nota-fiscal-lista',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatSnackBarModule
  ],
  templateUrl: './nota-fiscal-lista.html',
  styleUrl: './nota-fiscal-lista.scss'
})
export class NotaFiscalLista implements OnInit {
  notas: NotaFiscal[] = [];
  carregando = true;
  imprimindoId: number | null = null;
  colunasExibidas: string[] = ['numero', 'status', 'itens', 'acoes'];
  StatusNotaFiscal = StatusNotaFiscal;

  constructor(
    private notaFiscalService: NotaFiscalService,
    private cdr: ChangeDetectorRef,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    this.carregarNotas();
  }

  carregarNotas(): void {
    this.carregando = true;
    this.notaFiscalService.listar().subscribe({
      next: (notas) => {
        this.notas = notas;
        this.carregando = false;
        setTimeout(() => this.cdr.detectChanges());
      },
      error: (erro) => {
        console.error('Erro ao carregar notas fiscais:', erro);
        this.carregando = false;
        setTimeout(() => this.cdr.detectChanges());
      }
    });
  }

  imprimir(id: number): void {
    this.imprimindoId = id;
    this.notaFiscalService.imprimir(id).subscribe({
      next: () => {
        this.snackBar.open('Nota fiscal impressa com sucesso!', 'Fechar', { duration: 3000 });
        this.imprimindoId = null;
        this.carregarNotas();
      },
      error: (erro) => {
        this.imprimindoId = null;
        const mensagem = erro.error?.mensagem || 'Erro ao imprimir a nota fiscal.';
        this.snackBar.open(mensagem, 'Fechar', { duration: 5000 });
        setTimeout(() => this.cdr.detectChanges());
      }
    });
  }
}