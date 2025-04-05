using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models.ProductAndes
{
    using System;
    using System.Collections.Generic;

    public class ProductAndesModel
    {
        public string IDArticulo { get; set; }
        public string Nombre { get; set; }
        public int PrecioVenta { get; set; }
        public int Stock { get; set; }
        public int UltimoPrecioCosto { get; set; }
        public int UltimoPrecioCompra { get; set; }
        public int IDProveedor { get; set; }
        public string Proveedor { get; set; }
        public int IDGrupoArticulo { get; set; }
        public string Grupo { get; set; }
        public int IDSubGrupoArticulo { get; set; }
        public string SubGrupo { get; set; }
        public string Texto1 { get; set; }
        public string Descripcion { get; set; }
        public string Texto2 { get; set; }
        public string Modelo { get; set; }
        public string TextoFotoWeb { get; set; }
        public string NombreWeb { get; set; }
        public int SiTextoWeb { get; set; }
        public string TextoWeb { get; set; }
        public int IDMarca { get; set; }
        public string Marca { get; set; }
        public int IDEstado { get; set; }
        public int SiRegalo { get; set; }
        public string NombreInterno { get; set; }
        public int IDFamilia { get; set; }
        public int GrupoDescuento { get; set; }
        public string TextoExclusivo { get; set; }
        public int IDEstadoVentas { get; set; }
        public string Tags { get; set; }
        public string Comentario { get; set; }
        public int IDGrupoAgrupa { get; set; }
        public string GrupoAgrupa { get; set; }
        public int IDGrupoContableAgrupa { get; set; }
        public string GrupoContableAgrupa { get; set; }
        public int IDCategoriaAgrupa { get; set; }
        public string CategoriaAgrupa { get; set; }
        public int IDSubCategoriaAgrupa { get; set; }
        public string SubCategoriaAgrupa { get; set; }
        public string Metros { get; set; }
        public string CategorizacionWeb { get; set; }

        // Propiedad para manejar las imágenes
        public List<ImagenArticulo> Imagen { get; set; } 
    }

    public class ImagenArticulo
    {
        public string Imagen { get; set; } // Ejemplo: "130068.jpg"
        public string ToolTip { get; set; } // Nota: Corregí el nombre de "ToolTilp" a "ToolTip"
    }
}
