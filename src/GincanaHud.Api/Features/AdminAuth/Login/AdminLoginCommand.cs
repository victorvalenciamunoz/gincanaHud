using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.AdminAuth.Login;

public sealed record AdminLoginCommand(string Username, string Password)
	: IRequest<ErrorOr<AdminLoginResponse>>;
