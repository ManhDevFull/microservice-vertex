using System.Text.Json;
using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Model;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;

namespace be_dotnet_ecommerce1.Repository.IRepository
{
    public class VariantRepository : IVariantRepository
    {
        private readonly ConnectData _connect;
        public VariantRepository(ConnectData connect)
        {
            _connect = connect;
        }
        public async Task<List<V_VariantFilterDTO>> getAllVariant() // lấy ra tất cả các variant từ view from v_variant_filters
        {
            var data = await _connect.Database.SqlQueryRaw<V_VariantFilterDTO>(@"SELECT * from v_variant_filters")
            .ToListAsync();
            return data;
        }
        public async Task<List<VariantFilterDTO>> GetValueVariant()
        {
            var data = await _connect.Database.SqlQueryRaw<VariantFilterDTO>(@"SELECT * from v_variant_filters").ToListAsync();
            return data;
        }
        public async Task<List<VariantFilterDTO>> GetValueVariantByNameCategory(string? name)
        {
            // Bắt đầu một IQueryable, chưa thực thi
            var query = _connect.Set<V_variant>().AsQueryable();

            // 1. Làm sạch và kiểm tra đầu vào
            if (!string.IsNullOrEmpty(name))
            {
                var cleanedName = name.Trim(); // Loại bỏ khoảng trắng/ký tự xuống dòng
                                               // 2. Thêm điều kiện WHERE một cách an toàn
                query = query.Where(v => v.namecategory == cleanedName);
            }

            // 3. Thực thi truy vấn (EF Core tự động tạo SQL an toàn)
            var dataRow = await query.ToListAsync();

            // 4. Ánh xạ kết quả (phần này vẫn giữ nguyên)
            var rs = dataRow.Select(r => new VariantFilterDTO
            {
                id = r.id,
                namecategory = r.namecategory,
                brand = r.brand,
                variant = string.IsNullOrEmpty(r.variant)
                    ? null
                    : JsonSerializer.Deserialize<List<Dictionary<string, string[]>>>(r.variant)?.FirstOrDefault()
            }).ToList();
            return rs;
        }

        public async Task<List<Variant>> GetVariantByFilter(FilterDTO dTO) // done
        {
            var sql = "select * from variant";
            var conditions = new List<string>();
            var conditionsProduct = new Dictionary<string, List<string>>();
            var conditionsVariant = new Dictionary<string, List<string>>();

            if (dTO.Filter != null)
            {
                foreach (var entry in dTO.Filter)
                {
                    var key = item.Key;
                    var value = item.Value.ToList();
                    if (key == "brand" || key == "namecategory")
                        conditionsProduct[key] = value;
                    else
                        conditionsVariant[key] = value;
                }
            }

            if (conditions.Count > 0)
            {
                sql += " where " + string.Join(" AND ", conditions);
                Console.Write(sql);
            }
            var result = await _connect.variants.FromSqlRaw(sql).ToListAsync();
            return result;
        }
        public async Task<Variant[]> GetVariantByIdProduct(int id)
        {
            var result = await _connect.variants
                .Include(p => p.product)
                .Where(p => p.product != null && p.product.id == id)
                .ToArrayAsync();
            return result;
        }
        public async Task<Variant[]> getVariantByIdProducts(List<int> ids) // lấy danh sách variant by list product ids
        {
            // if (ids == null)
            //     return new Variant[0];
            // var rs = await _connect.variants.Where(v => ids.Contains(v.productid)).Distinct().ToArrayAsync();
            // return rs;
            return null;
        }
    }
}
