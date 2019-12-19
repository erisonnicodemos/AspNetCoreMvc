using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using static DevIO.Business.Models.Validation.Documentos.ValidacaoDocs;

namespace DevIO.Business.Models.Validation
{
    public class FornecedorValidation : AbstractValidator<Fornecedor>
    {
        public FornecedorValidation()
        {
            RuleFor(f => f.Nome)
                .NotEmpty().WithMessage("O campo {propertyName} precisa ser fornecido")
                .Length(2, 100)
                .WithMessage("O campo {propertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            When(f => f.TipoFornecedor == TipoFornecedor.PessoaFisica, () => {
                RuleFor(f => f.Documento.Length).Equal(CpfValidacao.TamanhoCpf)
                    .WithMessage("O Campo CPF precisa ter {ComparisonValue} caracteres e foi fornecido {PropertValue}");
                RuleFor(f => CpfValidacao.Validar(f.Documento)).Equal(true)
                    .WithMessage("O CPF fornecido é invalido");
            });

            When(f => f.TipoFornecedor == TipoFornecedor.PessoaJuridica, () => {
                RuleFor(f => f.Documento.Length).Equal(CnpjValidacao.TamanhoCnpj)
                    .WithMessage("O Campo CNPJ precisa ter {ComparisonValue} caracteres e foi fornecido {PropertValue}");
                RuleFor(f => CnpjValidacao.Validar(f.Documento)).Equal(true)
                    .WithMessage("O CNPJ fornecido é invalido");
            });



            When(f => f.TipoFornecedor == TipoFornecedor.PessoaJuridica, () => { });
        }
    }
}
