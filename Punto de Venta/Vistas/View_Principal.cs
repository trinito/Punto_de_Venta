﻿using Punto_de_Venta.Controlador;
using Punto_de_Venta.Modelo;
using Punto_de_Venta.Servicios;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Menu = Punto_de_Venta.Modelo.Menu;
namespace Punto_de_Venta.Vistas
{
    public partial class View_Principal : Form
    {

        #region VARIABLES Y PROPIEDADES
        private decimal total = 0;

        private decimal total_copia = 0;
        private decimal pago_copia = 0;
        private decimal cambio_copia = 0;
        private string forma_pago_copia = "";
       
        private List<ProductoCaja> productos;

        private List<ProductoCaja> productos_copia;

        #endregion
        public View_Principal()
        {
            InitializeComponent();
            productos = new List<ProductoCaja>();
        }

        #region EVENTOS
        private void View_Principal_Load(object sender, EventArgs e)
        {
            dgv_productos.AutoGenerateColumns = false;
            txt_producto.Focus();
        }

        private void txt_producto_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar ==(char)13 && !string.IsNullOrEmpty(txt_producto.Text))
            {
                try
                {
                    int cantidad_aux = 1;
                    string codigo_producto = txt_producto.Text;
                    string[] valor = codigo_producto.Split('*');   
                    if (valor != null && valor.Count() ==2)
                    {
                        cantidad_aux = Convert.ToInt32(valor.First());
                        codigo_producto = valor.Last();
                    }
                    else if(valor.Count()>2)
                    {
                        string message = "Error de código, favor de revisar que sea correcto";
                        string title = "¡Alerta!";
                        MessageBox.Show(message,title);
                        return;
                    }
                    if(lbl_cambio.Text!="$0")
                    {
                        lbl_cambio.Text = "$0";
                    }
                    Busqueda(codigo_producto, cantidad_aux);
                   
                 
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "ERROR");
                }
                finally
                {
                    Limpiar();
                }
            }
        }

        private void View_Principal_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.F4:
                    AbrirCobrar();
                    break;
                case Keys.F1:
                    AbrirBuscar();
                    break;
                case Keys.Delete:
                    EliminarProducto();
                    break;
                case Keys.F5:
                    ImprimirCopia();
                    break;

                default:
                    break;
            }
        }
        private void button_buscar_Click(object sender, EventArgs e)
        {
            AbrirBuscar();
        }

        private void button_cobrar_Click(object sender, EventArgs e)
        {
            //panel_pago.Visible = true;
            //groupbox_pago.Visible = true;
            AbrirCobrar();
        }

        private void button_quitar_Click(object sender, EventArgs e)
        {
            EliminarProducto();
        }

        #endregion

        #region METODOS

   
        void AbrirBuscar()
        {
            View_Buscar form = new View_Buscar();
            form.ShowDialog();

                if(form.productoSelect != null)
            {
                Busqueda(form.productoSelect.codigo, 1);
            }
            form.Dispose();
        }

        private void EliminarProducto()
        {
            if(dgv_productos.CurrentRow != null)
            {
                ProductoCaja producto = (ProductoCaja)dgv_productos.CurrentRow.DataBoundItem;
                dgv_productos.DataSource = null;
                productos.Remove(producto);
                dgv_productos.DataSource = productos;
                Totalizar();
            }
        }

        void AbrirCobrar()
        {
            if(total==0)
            {
                string message = "Para cobrar primero ingrese un producto";
                string title = "¡Mensaje!";
                MessageBox.Show(message, title);
                return;
            }
            View_Cobrar form = new View_Cobrar();
            form.total = this.total;
            form.ShowDialog();
            if(form.compra)
            {
                GuardarVenta(form.total, form.pago, form.cambio, form.forma_pago);
            }
            form.Dispose();
        }

       
        private void Busqueda(string codigo, int cantidad_aux)
        {
            ProductoCaja producto = productos.FirstOrDefault(x => x.codigo.Equals(codigo));


            if (producto == null)
            {
                producto = GetProducto(codigo);
                if (producto == null)
                {
                    string message = "No se encontró el producto, favor de buscar otro";
                    string title = "¡Mensaje!";
                    MessageBox.Show(message, title);
                    return;
                }
                else
                {
                    producto.cantidad = cantidad_aux;
                    producto.total = producto.cantidad * producto.precio;
                    productos.Add(producto);
                    Totalizar();
                }
            }
            else
            {
                producto.cantidad += cantidad_aux;
                producto.total = producto.precio * producto.cantidad;
                Totalizar();
            }
            dgv_productos.DataSource = null;
            dgv_productos.DataSource = productos;
        }

        private void Totalizar()
        {
            total = productos.Sum(x=>x.total);
            lbl_por_pagar.Text = total.ToString("C", CultureInfo.CurrentCulture);
        }

        private void Limpiar()
        {
            txt_producto.Text = "";
            txt_producto.Focus();
        }
        private void LimpiarTodo(decimal cambio)
        {
            total = 0;
            productos = new List<ProductoCaja>();
            dgv_productos.DataSource = null;
            lbl_por_pagar.Text = "$0";
            lbl_cambio.Text = cambio.ToString("C", CultureInfo.CurrentCulture); ;
            Limpiar();
           
        }

        private ProductoCaja GetProducto(string codigo)
        {
            ProductosController productosController = new ProductosController(new chinahousedbEntities());
            Menu producto = productosController.GetProducto(codigo);
            if(producto == null)
            {
                return null;
            }


             return ConvertMenu(producto);
            
        }

        private ProductoCaja ConvertMenu(Menu producto)
        {
            ProductoCaja productoCaja = new ProductoCaja 
            {id_menu = producto.id_menu ,codigo = producto.codigo, nombre = producto.nombre, medida = producto.medida, precio = producto.precio };


            return productoCaja;
        }

        private void GuardarVenta(decimal total, decimal pago, decimal cambio, string forma_pago)
        {
            //aqui guardamos en la BD
            int cantidad_productos = productos.Sum(x => x.cantidad);
            VentaController venta = new VentaController(new chinahousedbEntities());
            int result = venta.CrearVenta(DateTime.Now.ToString("dd/MM/yyyy"), DateTime.Now.ToString("hh:mm tt"), cantidad_productos, total, true, forma_pago);
            if(result == 0)
            {
                string message = "Error en la base de datos, no se pudo registrar la venta...";
                string title = "¡Alerta!";
                MessageBox.Show(message, title);
            }
            else
            {
                DetalleVentaController detalleVenta = new DetalleVentaController(new chinahousedbEntities());
                bool resultado = detalleVenta.CrearDetalleVenta(result, productos);
                if(resultado)
                {
                    total_copia = total;
                    pago_copia = pago;
                    cambio_copia = cambio;
                    forma_pago_copia = forma_pago;
                    productos_copia = productos;
                    ImprimirTicket(total, pago, cambio, forma_pago);
                }
                else
                {
                    string message = "Error en la base de datos, no se pudo registrar el detalle de venta...";
                    string title = "¡Alerta!";
                    MessageBox.Show(message, title);
                }
            }

            
        }

        private void ImprimirTicket(decimal total, decimal pago, decimal cambio, string forma_pago, bool isCopia = false)
        {
            try
            {
                if(!isCopia)
                {
                    printDocument1 = new PrintDocument();
                    PrinterSettings ps = new PrinterSettings();
                    printDocument1.PrinterSettings = ps;
                    printDocument1.PrintPage += Imprimir;
                    printDocument1.Print();
                }
                

                ImprimirTickets ticket = new ImprimirTickets();
                //ticket.TextoIzquierda(" ");
                //ticket.TextoCentro("SU RECIBO GRACIAS HASTA PRONTO");
                //ticket.TextoIzquierda(" ");
                ticket.TextoExtremos(DateTime.Now.ToString("dd/MM/yyyy"), DateTime.Now.ToString("hh:mm tt"));
                ticket.TextoIzquierda(" ");
                ticket.EncabezadoVenta();
                ticket.lineasGuio();
                if(isCopia)
                {
                    foreach (ProductoCaja producto in productos_copia)
                    {
                        ticket.AgregaArticulo(producto.nombre, producto.cantidad, producto.precio);
                    }
                }
                else
                {
                    foreach (ProductoCaja producto in productos)
                    {
                        ticket.AgregaArticulo(producto.nombre, producto.cantidad, producto.precio);
                    }
                }
               
                ticket.lineasGuio();
                ticket.TextoDerecha(forma_pago);
                ticket.AgregarTotales("               TOTAL:  ", total);
                ticket.AgregarTotales("            SU PAGO :  ", pago);
                ticket.AgregarTotales("              CAMBIO: ", cambio);
                //ticket.TextoIzquierda(" ");
                //ticket.TextoIzquierda(" ");
                ticket.TextoIzquierda(" ");
                if (isCopia)
                {
                    ticket.TextoIzquierda(" ");
                    ticket.TextoIzquierda(" ");
                    ticket.CortaTicket();
                }
                ticket.ImprimirTicket("ZJ-5890");
                if (!isCopia)
                {
                    printDocument1.PrintPage -= Imprimir;
                    printDocument1.PrintPage += ImprimirImagen;
                    printDocument1.Print();
                }
                LimpiarTodo(cambio);
            }
            catch (Exception eeee) { }

        }

        private void Imprimir(object sender, PrintPageEventArgs e)
        {
            int width = 195;
            int y = 20;

            Font font = new Font("Algerian", 15, FontStyle.Regular, GraphicsUnit.Point);
            StringFormat drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Center;
            e.Graphics.DrawString("SU RECIBO GRACIAS \n HASTA PRONTO", font, Brushes.Black, new RectangleF(0, 0, 195, 100), drawFormat);
        }
        private void ImprimirImagen(object sender, PrintPageEventArgs e)
        {
            Image image = Image.FromFile(@"C:\Users\Administrador\source\repos\Punto de Venta\Punto de Venta\Resources\china_logo.jpg");
            e.Graphics.DrawImage(image, new Rectangle(0, 0, 210, 100));
        }


        #endregion

        private void button_copia_Click(object sender, EventArgs e)
        {
            ImprimirCopia();
        }

        private void ImprimirCopia()
        {
            if(total_copia != 0 && pago_copia !=0)
            {
                ImprimirTicket(total_copia, pago_copia, cambio_copia, forma_pago_copia, true);
                total_copia = 0; pago_copia = 0; cambio_copia=0;
            }
           
        }
    }
}
