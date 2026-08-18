import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { ProdutoService } from '../../services/produto.service';
import { Produto } from '../../models/produto';

@Component({
  selector: 'app-nota-fiscal-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './nota-fiscal-form.html',
  styleUrl: './nota-fiscal-form.scss'
})
export class NotaFiscalForm implements OnInit {
  form: FormGroup;
  produtos: Produto[] = [];
  enviando = false;

  constructor(
    private fb: FormBuilder,
    private notaFiscalService: NotaFiscalService,
    private produtoService: ProdutoService,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      itens: this.fb.array([])
    });
  }

  ngOnInit(): void {
    this.produtoService.listar().subscribe({
      next: (produtos) => {
        this.produtos = produtos;
        setTimeout(() => this.cdr.detectChanges());
      },
      error: (erro) => console.error('Erro ao carregar produtos:', erro)
    });
    this.adicionarItem();
  }

  get itens(): FormArray {
    return this.form.get('itens') as FormArray;
  }

  adicionarItem(): void {
    const item = this.fb.group({
      produtoId: ['', Validators.required],
      quantidade: [1, [Validators.required, Validators.min(1)]]
    });
    this.itens.push(item);
  }

  removerItem(index: number): void {
    this.itens.removeAt(index);
  }

  salvar(): void {
    if (this.form.invalid || this.itens.length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    this.enviando = true;
    this.notaFiscalService.criar(this.form.value).subscribe({
      next: () => {
        this.snackBar.open('Nota fiscal criada com sucesso!', 'Fechar', { duration: 3000 });
        this.router.navigate(['/notas-fiscais']);
      },
      error: (erro) => {
        console.error('Erro ao criar nota fiscal:', erro);
        this.snackBar.open('Erro ao criar nota fiscal. Tente novamente.', 'Fechar', { duration: 3000 });
        this.enviando = false;
        setTimeout(() => this.cdr.detectChanges());
      }
    });
  }

  cancelar(): void {
    this.router.navigate(['/notas-fiscais']);
  }
}