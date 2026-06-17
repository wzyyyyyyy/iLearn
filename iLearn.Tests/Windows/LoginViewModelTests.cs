using iLearn.Models;
using iLearn.Notifications;
using iLearn.Security;
using iLearn.Services;
using iLearn.ViewModels.Windows;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace iLearn.Tests.Windows;

public sealed class LoginViewModelTests
{
    [Fact]
    public async Task SubmitCommand_UsesPasswordLogin_WhenOnPasswordStep()
    {
        var api = new FakeILearnApiService { Step1Result = LoginStepResult.WrongPassword };
        var viewModel = CreateViewModel(api);
        viewModel.UserName = "student";
        viewModel.UserPassword = "password";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(1, api.Step1Calls);
        Assert.Equal(0, api.Step2Calls);
        Assert.Equal(LoginStep.Password, viewModel.CurrentStep);
        Assert.Equal("验证并登录", viewModel.SubmitButtonText);
    }

    [Fact]
    public async Task SubmitCommand_UsesWechatVerification_WhenOnWechatStep()
    {
        var api = new FakeILearnApiService
        {
            Step1Result = LoginStepResult.NeedWechatCode,
            Step2Result = LoginStepResult.WrongPassword
        };
        var viewModel = CreateViewModel(api);
        viewModel.UserName = "student";
        viewModel.UserPassword = "password";

        await viewModel.SubmitCommand.ExecuteAsync(null);
        viewModel.ImageCaptchaCode = "abcd";
        viewModel.WechatCode = "123456";
        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(1, api.Step1Calls);
        Assert.Equal(1, api.Step2Calls);
        Assert.Equal("提交微信验证码", viewModel.SubmitButtonText);
    }

    [Fact]
    public async Task SubmitButtonText_Changes_WhenWechatVerificationIsRequired()
    {
        var api = new FakeILearnApiService { Step1Result = LoginStepResult.NeedWechatCode };
        var viewModel = CreateViewModel(api);
        viewModel.UserName = "student";
        viewModel.UserPassword = "password";

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(LoginStep.WechatVerify, viewModel.CurrentStep);
        Assert.Equal("提交微信验证码", viewModel.SubmitButtonText);
        Assert.True(viewModel.IsWechatVerifyStep);
    }

    private static LoginViewModel CreateViewModel(FakeILearnApiService api)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new LoginViewModel(
            api,
            new NotificationService(),
            new InMemorySecretStore(),
            new AppConfig(),
            services);
    }

    private sealed class FakeILearnApiService : ILearnApiService
    {
        public int Step1Calls { get; private set; }
        public int Step2Calls { get; private set; }
        public LoginStepResult Step1Result { get; set; } = LoginStepResult.Success;
        public LoginStepResult Step2Result { get; set; } = LoginStepResult.Success;

        public override Task<LoginStepResult> LoginStep1Async(string username, string password)
        {
            Step1Calls++;
            return Task.FromResult(Step1Result);
        }

        public override Task<LoginStepResult> LoginStep2Async(string imageCaptcha, string wechatCode)
        {
            Step2Calls++;
            return Task.FromResult(Step2Result);
        }

        public override Task<byte[]> GetCasCaptchaBytesAsync()
        {
            return Task.FromResult(Array.Empty<byte>());
        }
    }

    private sealed class InMemorySecretStore : ISecretStore
    {
        private readonly Dictionary<string, string> _secrets = [];

        public Task<string?> ReadSecretAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_secrets.GetValueOrDefault(key));
        }

        public Task SaveSecretAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _secrets[key] = value;
            return Task.CompletedTask;
        }

        public Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
        {
            _secrets.Remove(key);
            return Task.CompletedTask;
        }
    }
}
