using System.Text.Json;
using be_dotnet_ecommerce1.Data;
using dotnet.Dtos;
using dotnet.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace dotnet.Repository;

public class CartRepository : ICartRepository
{
    private readonly ConnectData _db;

    public CartRepository(ConnectData db)
    {
        _db = db;
    }

    public async Task<CartResponseDto> GetCartAsync(int accountId)
    {
        var items = await (from sc in _db.shoppingCarts
                           join v in _db.variants on sc.variantid equals v.id
                           join p in _db.products on v.product_id equals p.id
                           where sc.accountid == accountId && !p.isdeleted && !v.isdeleted
                           orderby sc.id
                           select new
                           {
                               cartId = sc.id,
                               variantId = v.id,
                               productId = p.id,
                               productName = p.nameproduct,
                               imageUrl = p.imageurls.FirstOrDefault(),
                               valueVariant = v.valuevariant,
                               unitPrice = v.price,
                               quantity = sc.quantity
                           }).ToListAsync();

        var now = DateTime.UtcNow;

        var discountPerItem = await (from sc in _db.shoppingCarts
                                     join v in _db.variants on sc.variantid equals v.id
                                     join dp in _db.discountProducts on v.id equals dp.variantid into dpj
                                     from dp in dpj.DefaultIfEmpty()
                                     join d in _db.discounts on dp.discountid equals d.id into dj
                                     from d in dj.DefaultIfEmpty()
                                     where sc.accountid == accountId
                                     select new
                                     {
                                         sc.variantid,
                                         sc.quantity,
                                         v.price,
                                         type = (int?)d.typediscount,
                                         value = (int?)d.discount,
                                         start = (DateTime?)d.starttime,
                                         end = (DateTime?)d.endtime
                                     }).ToListAsync();

        decimal discountTotal = 0m;
        foreach (var row in discountPerItem)
        {
            if (row.value == null || row.start == null || row.end == null) continue;
            if (now < row.start.Value || now > row.end.Value) continue;

            if (row.type == 1)
            {
                discountTotal += (decimal)(row.price * row.quantity) * (decimal)row.value.Value / 100m;
            }
            else
            {
                discountTotal += (decimal)row.value.Value * row.quantity;
            }
        }

        var itemDtos = items.Select(x => new CartItemDto
        {
            cartId = x.cartId,
            variantId = x.variantId,
            productId = x.productId,
            productName = x.productName,
            imageUrl = x.imageUrl,
            color = CartItemDto.ExtractColor(x.valueVariant != null ? x.valueVariant.RootElement.GetRawText() : null),
            unitPrice = x.unitPrice,
            quantity = x.quantity
        }).ToList();

        var itemsPrice = itemDtos.Sum(i => (decimal)i.unitPrice * i.quantity);

        return new CartResponseDto
        {
            items = itemDtos,
            summary = new CartSummaryDto
            {
                itemsPrice = itemsPrice,
                discountPrice = discountTotal,
                totalPrice = itemsPrice - discountTotal
            }
        };
    }

    public async Task<CartResponseDto> AddOrIncrementAsync(int accountId, int variantId, int quantityDelta)
    {
        var existing = await _db.shoppingCarts.FirstOrDefaultAsync(x => x.accountid == accountId && x.variantid == variantId);
        if (existing == null)
        {
            await _db.shoppingCarts.AddAsync(new dotnet.Model.ShoppingCart
            {
                accountid = accountId,
                variantid = variantId,
                quantity = Math.Max(1, quantityDelta)
            });
        }
        else
        {
            existing.quantity = Math.Max(1, existing.quantity + quantityDelta);
            _db.shoppingCarts.Update(existing);
        }

        await _db.SaveChangesAsync();
        return await GetCartAsync(accountId);
    }

    public async Task<CartResponseDto> UpdateQuantityAsync(int accountId, int cartId, int quantity)
    {
        var cart = await _db.shoppingCarts.FirstOrDefaultAsync(x => x.id == cartId && x.accountid == accountId);
        if (cart == null)
        {
            return await GetCartAsync(accountId);
        }

        if (quantity <= 0)
        {
            _db.shoppingCarts.Remove(cart);
        }
        else
        {
            cart.quantity = quantity;
            _db.shoppingCarts.Update(cart);
        }

        await _db.SaveChangesAsync();
        return await GetCartAsync(accountId);
    }

    public async Task<CartResponseDto> RemoveAsync(int accountId, int cartId)
    {
        var cart = await _db.shoppingCarts.FirstOrDefaultAsync(x => x.id == cartId && x.accountid == accountId);
        if (cart != null)
        {
            _db.shoppingCarts.Remove(cart);
            await _db.SaveChangesAsync();
        }

        return await GetCartAsync(accountId);
    }

    public async Task<List<CartSuggestionDto>> GetSuggestionsAsync(int accountId, int offset, int limit, List<int> excludeProductIds)
    {
        var inCartIds = await (from sc in _db.shoppingCarts
                               join v in _db.variants on sc.variantid equals v.id
                               join p in _db.products on v.product_id equals p.id
                               where sc.accountid == accountId && !p.isdeleted
                               select p.id).Distinct().ToListAsync();

        var categories = await (from sc in _db.shoppingCarts
                                join v in _db.variants on sc.variantid equals v.id
                                join p in _db.products on v.product_id equals p.id
                                where sc.accountid == accountId && !p.isdeleted
                                select p.categoryId).Distinct().ToListAsync();

        var excludeSet = new HashSet<int>(inCartIds);
        if (excludeProductIds != null)
        {
            excludeSet.UnionWith(excludeProductIds);
        }

        var dtoQuery = from p in _db.products
                       join c in _db.categories on p.categoryId equals c.id into cj
                       from c in cj.DefaultIfEmpty()
                       where categories.Contains(p.categoryId)
                             && !p.isdeleted
                             && !excludeSet.Contains(p.id)
                       orderby p.updatedate descending, p.createdate descending
                       select new CartSuggestionDto
                       {
                           productId = p.id,
                           productName = p.nameproduct,
                           imageUrl = p.imageurls.FirstOrDefault(),
                           minPrice = _db.variants
                               .Where(v => v.product_id == p.id && !v.isdeleted)
                               .Select(v => (decimal?)v.price)
                               .OrderBy(x => x)
                               .FirstOrDefault() ?? 0m,
                           discountPercent = (from v in _db.variants
                                              join dp in _db.discountProducts on v.id equals dp.variantid
                                              join d in _db.discounts on dp.discountid equals d.id
                                              where v.product_id == p.id && !v.isdeleted && d.typediscount == 2
                                              select (int?)d.discount).Max(),
                           fixedDiscount = (from v in _db.variants
                                            join dp in _db.discountProducts on v.id equals dp.variantid
                                            join d in _db.discounts on dp.discountid equals d.id
                                            where v.product_id == p.id && !v.isdeleted && d.typediscount == 1
                                            select (int?)d.discount).Max(),
                           categoryName = c != null ? c.namecategory : string.Empty
                       };

        var result = await dtoQuery.Skip(offset).Take(limit).ToListAsync();

        foreach (var item in result)
        {
            var original = item.minPrice;
            var best = original;
            int? effectivePercent = null;

            if (item.discountPercent.HasValue && item.discountPercent.Value > 0)
            {
                var percent = Math.Clamp(item.discountPercent.Value, 0, 100);
                best = Math.Min(best, Math.Round(original * (100 - percent) / 100m, 2));
                effectivePercent = percent;
            }

            if (item.fixedDiscount.HasValue && item.fixedDiscount.Value > 0)
            {
                var after = Math.Max(0m, original - item.fixedDiscount.Value);
                if (after < best)
                {
                    best = after;
                    effectivePercent = (int)Math.Round((original - after) * 100m / (original == 0 ? 1 : original));
                }
            }

            if (best < original)
            {
                item.oldPrice = original;
                item.minPrice = best;
                item.discountPercent = effectivePercent;
            }

            item.fixedDiscount = null;
        }

        return result;
    }
}

