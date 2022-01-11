﻿using Punto_de_Venta.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Punto_de_Venta.Controlador
{
    public class VentaController
    {
        private readonly chinahousedbEntities Context;

        public VentaController(chinahousedbEntities context)
        {
            Context = context;
        }

        public int CrearVenta(string fecha, string hora, int cantidad_productos, decimal total, bool estatus, string forma_pago)
        {
            int result = 0;
            try
            {
                Venta venta = new Venta { fecha = fecha, hora=hora, cantidad_productos = cantidad_productos, total = total, estatus = estatus, forma_pago = forma_pago };
                Context.Venta.Add(venta);
                int x = Context.SaveChanges();
                if (x > 0)
                {
                     result = venta.id_venta;
                }
                else
                    return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en la base de datos, no se pudo registrar la venta.");
            }

            return result;
        }
    }
}
