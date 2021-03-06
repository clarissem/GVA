﻿using GerenciamentoDeClientes.Dominio;
using GerenciamentoDeClientes.Negocio;
using GerenciamentoDeClientes.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace GerenciamentoDeClientes
{
    public partial class FormGrafico : Form
    {
        private bool dataInicial = false;

        private bool dataFinal = false;


        public FormGrafico()
        {
            InitializeComponent();
            GerarGraficos();
        }

        private void FormGrafico_Load(object sender, EventArgs e)
        {
        }

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            if (dataInicial)
                Util.ValidacaoCampo.AlteraBordaControl(txtDataInicial, e);

            if (dataFinal)
                Util.ValidacaoCampo.AlteraBordaControl(txtDataFinal, e);
        }

        private void GerarGraficos()
        {
            try
            {
                lblNotificacao.Text = string.Empty;
                dataInicial = false;
                dataFinal = false;

                var filtro = new FiltroTelaVendas()
                {
                    DataInicial = String.IsNullOrWhiteSpace(txtDataInicial.Text.Replace("_", "").Replace("/", "")) ? (DateTime?)null : DateTime.Parse(txtDataInicial.Text),
                    DataFinal = String.IsNullOrWhiteSpace(txtDataFinal.Text.Replace("_", "").Replace("/", "")) ? (DateTime?)null : DateTime.Parse(txtDataFinal.Text),
                };

                if (filtro.DataFinal.HasValue && filtro.DataInicial.HasValue && filtro.DataFinal.Value < filtro.DataInicial.Value)
                    lblNotificacao.Text = "A data inicial deve ser menor que a data final.";
                else
                {
                    var vendas = new CadastroVenda().BuscaVendasComFiltro(filtro).ToList();

                    var dt = BuscarDtGraficoVendas(vendas);
                    var dtClientes = BuscarClientes(vendas);

                    GraficoSpline(graficoVendasPorMes, dt, "Quantidade de Vendas por mês", "Mes", "Qtd");
                    GraficoSpline(gfcValorVendidoMes, dt, "Valores vendidos por mês", "Mes", "Valor");
                    GraficoClientesQueMaisCompram(dtClientes.OrderByDescending(c => c.QuantidadeCompras).Take(5).ToList());
                    GraficoClientesQueMaisGerarValor(dtClientes.OrderByDescending(c => c.ValorComprado).Take(5).ToList());
                }
            }
            catch (FormatException ex)
            {
                if (ex.Message.Contains("DateTime"))
                {
                    lblNotificacao.Text = Resources.DataInvalida;

                    if (!String.IsNullOrWhiteSpace(txtDataInicial.Text.Replace("_", "").Replace("/", "")) && DataInvalida(txtDataInicial.Text.Replace("_", "")))
                        dataInicial = true;

                    if (!String.IsNullOrWhiteSpace(txtDataFinal.Text.Replace("_", "").Replace("/", "")) && DataInvalida(txtDataFinal.Text.Replace("_", "")))
                        dataFinal = true;
                }
            
            }
            catch (Exception ex)
            {
                lblNotificacao.Text = ex.Message;
            }

            this.Refresh();

        }

        private DataTable BuscarDtGraficoVendas(List<Venda> vendas)
        {
            var vendasAgrupadas = from p in vendas
                                  group p by new
                                  {
                                      p.DataVenda.Month,
                                      p.DataVenda.Year
                                  } into grouping
                                  select new
                                  {
                                      grouping.Key,
                                      Qtd = grouping.Count(),
                                      Preco = grouping.Sum(p => p.ValorTotal)
                                  };

            var dt = new DataTable();

            dt.Columns.Add("Mes", typeof(String));
            dt.Columns.Add("Qtd", typeof(String));
            dt.Columns.Add("Valor", typeof(String));

            vendasAgrupadas = vendasAgrupadas.OrderBy(v => v.Key.Month);
            foreach (var valor in vendasAgrupadas)
            {
                dt.Rows.Add(valor.Key.Month + "/" + valor.Key.Year, valor.Qtd, valor.Preco);
            }

            return dt;
        }

        private List<GraficoClientes> BuscarClientes(List<Venda> vendas)
        {
            var topclientes = from c in vendas
                              group c by new { c.Cliente.Codigo, c.Cliente.Nome }
                                into grouping
                              select new
                              {
                                  grouping.Key,
                                  QtdCompras = grouping.Count(),
                                  Valor = grouping.Sum(p => p.ValorTotal)
                              };

            var lista = new List<GraficoClientes>();

            foreach (var item in topclientes)
            {
                lista.Add(new GraficoClientes(item.Key.Nome, item.QtdCompras, item.Valor));
            }

            return lista;
        }

        private void GraficoSpline(Chart grafico, DataTable dt, string titulo, string xmember, string ymember)
        {
            grafico.Titles.Clear();
            grafico.Titles.Add(titulo);
            grafico.DataSource = dt.DefaultView;
            grafico.Series["Series1"].XValueMember = xmember;
            grafico.Series["Series1"].YValueMembers = ymember;
            grafico.DataBind();
        }

        private void GraficoClientesQueMaisCompram(List<GraficoClientes> valores)
        {
            IniciarGrafico(gfcClientes, "Clientes que compram mais");

            foreach (var item in valores)
            {
                DataPoint p = new DataPoint(0, item.QuantidadeCompras);
                p.LegendText = item.NomeCliente;
                p.AxisLabel = item.QuantidadeCompras.ToString();
                gfcClientes.Series["Series1"].Points.Add(p);
            }
        }

        private void GraficoClientesQueMaisGerarValor(List<GraficoClientes> valores)
        {
            IniciarGrafico(gfcClientesGeramValor, "Clientes que geram mais valor");

            foreach (var item in valores)
            {
                DataPoint p = new DataPoint(0, item.ValorComprado);
                p.LegendText = item.NomeCliente;
                p.AxisLabel = item.ValorComprado.ToString();
                gfcClientesGeramValor.Series["Series1"].Points.Add(p);
            }
        }

        private void IniciarGrafico(Chart grafico, string titulo)
        {
            grafico.Titles.Clear();
            grafico.Titles.Add(titulo);
            grafico.Series["Series1"].Points.Clear();
        }

        private void btnPesquisar_Click(object sender, EventArgs e)
        {
            GerarGraficos();
        }

        private void gfcClientes_Click(object sender, EventArgs e)
        {

        }

        private void gfcClientesGeramValor_Click(object sender, EventArgs e)
        {

        }

        private bool DataInvalida(string adata)
        {
            DateTime ldata;
            DateTime.TryParse(adata, out ldata);

            return ldata == DateTime.MinValue;
        }
    }
}
