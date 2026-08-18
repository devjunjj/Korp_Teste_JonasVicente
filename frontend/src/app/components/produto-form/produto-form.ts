import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ProdutoService } from '../../services/produto.service';

@Component({
  selector: 'app-produto-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './produto-form.html',
  styleUrl: './produto-form.scss'
})
export class ProdutoForm {
  form: FormGroup;
  enviando = false;
  sugerindo = false;

  constructor(
    private fb: FormBuilder,
    private produtoService: ProdutoService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.form = this.fb.group({
      codigo: ['', [Validators.required, Validators.maxLength(20)]],
      descricao: ['', [Validators.required, Validators.maxLength(100)]],
      saldo: [0, [Validators.required, Validators.min(0)]]
    });
  }

  sugerirDescricao(): void {
    const codigo = this.form.get('codigo')?.value;

    if (!codigo) {
      this.snackBar.open('Preencha o código do produto antes de pedir uma sugestão.', 'Fechar', { duration: 3000 });
      return;
    }

    this.sugerindo = true;
    this.produtoService.sugerirDescricao(codigo).subscribe({
      next: (resposta) => {
        this.form.patchValue({ descricao: resposta.descricaoSugerida });
        this.sugerindo = false;
      },
      error: (erro) => {
        console.error('Erro ao sugerir descrição:', erro);
        const mensagem = erro.error?.mensagem || 'Não foi possível gerar a sugestão.';
        this.snackBar.open(mensagem, 'Fechar', { duration: 3000 });
        this.sugerindo = false;
      }
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.enviando = true;
    this.produtoService.criar(this.form.value).subscribe({
      next: () => {
        this.snackBar.open('Produto cadastrado com sucesso!', 'Fechar', { duration: 3000 });
        this.router.navigate(['/produtos']);
      },
      error: (erro) => {
        console.error('Erro ao cadastrar produto:', erro);
        this.snackBar.open('Erro ao cadastrar produto. Tente novamente.', 'Fechar', { duration: 3000 });
        this.enviando = false;
      }
    });
  }

  cancelar(): void {
    this.router.navigate(['/produtos']);
  }
}