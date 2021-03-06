﻿using GerenciamentoDeClientes.Dados.Contratos;
using GerenciamentoDeClientes.Dominio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GerenciamentoDeClientes.Dados
{
    public class RepositorioVenda : IRepositorioVenda
    {
        private const string NomeArquivo = "vendas.txt";
        private const char Separador = '|';

        public RepositorioVenda()
        {
            File.AppendAllText(NomeArquivo, "");
        }

        public bool Cria(Venda venda)
        {
            VerificarVenda(venda);

            venda.Codigo = BuscaProximoId();
            var consultaSerializado = string.Join(Separador.ToString(), venda.Codigo, venda.DataPagamento, venda.Cliente.Codigo, venda.Descricao, venda.DataVenda, venda.ValorTotal, venda.DataVencimento);
            File.AppendAllText(NomeArquivo, consultaSerializado + "\r\n");

            return true;
        }

        private bool VerificarVenda(Venda venda)
        {
            var valid = true;

            if (venda.DataVencimento < venda.DataVenda)
                valid = false;
            else if (venda.DataPagamento.HasValue && venda.DataPagamento.Value < venda.DataVenda)
                valid = false;
            else if (venda.DataVenda > DateTime.Now)
                valid = false;

            return valid;
        }

        public IEnumerable<Venda> BuscaVendasComFiltro(FiltroTelaVendas venda)
        {
            var lvendas = BuscaTodas();

            if (venda.CodigoCliente > 0)
                lvendas = lvendas.Where(v => v.Cliente.Codigo == venda.CodigoCliente);

            if (venda.DataFinal.HasValue)
                lvendas = lvendas.Where(v => v.DataVenda <= venda.DataFinal.Value);

            if (venda.DataInicial.HasValue)
                lvendas = lvendas.Where(v => v.DataVenda >= venda.DataInicial.Value);

            if (venda.Status == 2)
                lvendas = lvendas.Where(v => v.DataPagamento.HasValue);
            else if (venda.Status == 3)
                lvendas = lvendas.Where(v => v.DataVencimento < DateTime.Now && !v.DataPagamento.HasValue);
            else if (venda.Status == 1)
                lvendas = lvendas.Where(v => v.DataVencimento >= DateTime.Now && !v.DataPagamento.HasValue);

            return lvendas;
        }

        public Venda BuscaVendaPorCodigo(int codigo)
        {
            return BuscaTodas().FirstOrDefault(p => p.Codigo == codigo);
        }

        public IEnumerable<Venda> BuscaVendasPorCliente(int codigoCliente)
        {
            return BuscaTodas().Where(p => p.Cliente.Codigo == codigoCliente);
        }

        private int BuscaProximoId()
        {
            var consulta = BuscaTodas();
            return consulta.Any() ? consulta.Max(m => m.Codigo) + 1 : 1;
        }

        public IEnumerable<Venda> BuscaTodas()
        {
            var linhas = File.ReadAllLines(NomeArquivo);
            var repositorioCliente = new RepositorioCliente();

            foreach (var linha in linhas)
            {
                var valores = linha.Split(Separador);
                yield return new Venda
                {
                    Codigo = int.Parse(valores[0]),
                    DataPagamento = String.IsNullOrEmpty(valores[1]) ? null : (DateTime?)Convert.ToDateTime(valores[1]) ,
                    Cliente = repositorioCliente.BuscaPorCodigo(int.Parse(valores[2])),
                    Descricao = valores[3],
                    DataVenda = DateTime.Parse(valores[4]),
                    ValorTotal = double.Parse(valores[5]),
                    DataVencimento = DateTime.Parse(valores[6]),
                    //Status = int.Parse(valores[7]),
                };
            }
        }

        public Venda BuscaPorCodigo(int cod)
        {
            return BuscaTodas().FirstOrDefault(p => p.Codigo == cod);
        }

        public void ApagaVenda(int codigo)
        {
            var linhas = File.ReadAllLines(NomeArquivo);

            File.WriteAllLines(NomeArquivo, linhas.Where(l => l.Split(Separador)[0] != codigo.ToString()));
        }

        private static int BuscaStatus(Venda venda)
        {
            int lstatus = 1;  //Pendente

            if (venda.DataPagamento.HasValue)
                lstatus = 2; //Pago
            else if (venda.DataVencimento < DateTime.Now && !venda.DataPagamento.HasValue)
                lstatus = 3; //Vencido
            return lstatus;
        }
    }
}
