using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Model
{
    public class OrderDetail
    {

        public int id { get; set; }
        public int idorder { get; set; }
        public int idvariant { get; set; }
        public int quantity { get; set; }
        public virtual Order? order { get; set; }
        public virtual Variant? variant { get; set; }
    }
}