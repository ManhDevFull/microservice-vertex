using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Dtos;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;
using System.Text.Json;

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
    }
}
