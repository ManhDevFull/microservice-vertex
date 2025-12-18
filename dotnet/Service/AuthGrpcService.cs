using Dotnet.Grpc;
using Grpc.Core;
using be_dotnet_ecommerce1.Data;
using Microsoft.EntityFrameworkCore;

namespace dotnet.Service;

public class AuthGrpcService : AuthGrpc.AuthGrpcBase
{
    private readonly ConnectData _db;

    public AuthGrpcService(ConnectData db)
    {
        _db = db;
    }

    public override async Task<UserReply> GetUserById(UserRequest request, ServerCallContext context)
    {
        var user = await _db.accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.id == request.Id);

        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
        }

        return new UserReply
        {
            Id = user.id,
            Email = user.email ?? string.Empty,
            Firstname = user.firstname ?? string.Empty,
            Lastname = user.lastname ?? string.Empty
        };
    }
}
