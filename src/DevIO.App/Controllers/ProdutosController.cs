using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using DevIO.App.ViewModels;
using DevIO.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using DevIO.Business.Interfaces;
using DevIO.App.Extensions;

namespace DevIO.App.Controllers
{
    [Authorize]
    public class ProdutosController : BaseController
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IFornecedorRepository _fornecedorRepository;
        private readonly IProdutoService _produtoService;
        private readonly IMapper _mapper;

        public ProdutosController(IProdutoRepository produtoRepository,
                                  IFornecedorRepository fornecedorRepository,
                                  IMapper mapper, IProdutoService produtoService,
                                  INotificador notificador) : base(notificador)
        {
            _produtoRepository = produtoRepository;
            _fornecedorRepository = fornecedorRepository;
            _mapper = mapper;
            _produtoService = produtoService;
        }

        [AllowAnonymous]
        [Route("lista-de-produtos")]
        public async Task<IActionResult> Index()
        {
            return View(_mapper.Map<IEnumerable<ProdutoViewModel>>(await _produtoRepository.ObterProdutosFornecedores()));
        }

        [AllowAnonymous]
        [Route("detalhes-do-produto/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var produtoViewModel = await ObterProduto(id);
            if (produtoViewModel == null) return NotFound();

            return View(produtoViewModel);
        }

        [ClaimsAuthorize("Produto","Adicionar")]
        [Route("cadastrar-produto")]
        public async Task<IActionResult> Create()
        {
            var produtoViewModel = await PopularFornecedores(new ProdutoViewModel());

            return View(produtoViewModel);
        }

        [ClaimsAuthorize("Produto", "Adicionar")]
        [Route("cadastrar-produto")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProdutoViewModel produtoViewModel)
        {
            produtoViewModel = await PopularFornecedores(produtoViewModel);
            if (!ModelState.IsValid) return View(produtoViewModel);

            var imgPrefixo = Guid.NewGuid() + "_";
            if (!await UploadArquivo(produtoViewModel.ImagemUpload, imgPrefixo))
            {
                return View(produtoViewModel);
            }

            produtoViewModel.Imagem = imgPrefixo + produtoViewModel.ImagemUpload.FileName;
            await _produtoService.Adicionar(_mapper.Map<Produto>(produtoViewModel));

            if (!OperacaoValida()) return View(produtoViewModel);

            return RedirectToAction("Index");
        }

        [ClaimsAuthorize("Produto", "Editar")]
        [Route("editar-produto/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var produtoViewModel = await ObterProduto(id);
            if (produtoViewModel == null) return NotFound();

            return View(produtoViewModel);
        }

        [ClaimsAuthorize("Produto", "Editar")]
        [Route("editar-produto/{id:guid}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ProdutoViewModel produtoViewModel)
        {
            if (id != produtoViewModel.Id) return NotFound();

            var produtoAtuliazacao = await ObterProduto(id);
            produtoViewModel.Fornecedor = produtoAtuliazacao.Fornecedor;
            produtoViewModel.Imagem = produtoAtuliazacao.Imagem;

            if (!ModelState.IsValid) return View(produtoViewModel);

            if (produtoViewModel.ImagemUpload != null)
            {
                var imgPrefixo = Guid.NewGuid() + "_";

                if (!await UploadArquivo(produtoViewModel.ImagemUpload, imgPrefixo))
                {
                    return View(produtoViewModel);
                }

                produtoAtuliazacao.Imagem = imgPrefixo + produtoViewModel.ImagemUpload.FileName;
            }

            produtoAtuliazacao.Nome = produtoViewModel.Nome;
            produtoAtuliazacao.Descricao = produtoViewModel.Descricao;
            produtoAtuliazacao.Valor = produtoViewModel.Valor;
            produtoAtuliazacao.Ativo = produtoViewModel.Ativo;

            await _produtoService.Atualizar(_mapper.Map<Produto>(produtoAtuliazacao));
            if (!OperacaoValida()) return View(produtoViewModel);

            return RedirectToAction("Index");
        }

        [ClaimsAuthorize("Produto", "Excluir")]
        [Route("deletar-produto/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var produto = await ObterProduto(id);

            if (produto == null) return NotFound();

            return View(produto);
        }

        [ClaimsAuthorize("Produto", "Excluir")]
        [Route("deletar-produto/{id:guid}")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var produto = await ObterProduto(id);

            if (produto == null) return NotFound();

            await _produtoService.Remover(id);

            if (!OperacaoValida()) return View(produto);

            TempData["Sucesso"] = "Produto excluído com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        #region METHODS
        private async Task<ProdutoViewModel> ObterProduto(Guid id)
        {
            var produto = _mapper.Map<ProdutoViewModel>(await _produtoRepository.ObterProdutoPorFornecedor(id));
            produto.Fornecedores = _mapper.Map<IEnumerable<FornecedorViewModel>>(await _fornecedorRepository.ObterTodos());
            return produto;
        }

        private async Task<ProdutoViewModel> PopularFornecedores(ProdutoViewModel produto)
        {
            produto.Fornecedores = _mapper.Map<IEnumerable<FornecedorViewModel>>(await _fornecedorRepository.ObterTodos());
            return produto;
        }

        private async Task<bool> UploadArquivo(IFormFile arquivo, string imgPrefixo)
        {
            if (arquivo.Length <= 0) return false;

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", imgPrefixo + arquivo.FileName);

            if (System.IO.File.Exists(path))
            {
                ModelState.AddModelError(string.Empty, "Já existe um arquivo com este nome!");
                return false;
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return true;
        }
        #endregion

    }
}
