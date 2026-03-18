using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Services;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;
using Xunit;

namespace Redact1.Tests.ViewModels
{
    public class NewRequestViewModelTests : IDisposable
    {
        private readonly TestServiceProvider _services;

        public NewRequestViewModelTests()
        {
            _services = new TestServiceProvider(isAuthenticated: true);
            _services.SetupApp();
        }

        public void Dispose() => _services.Dispose();

        [Fact]
        public void Constructor_GeneratesRequestNumber()
        {
            var vm = _services.GetService<NewRequestViewModel>();

            vm.RequestNumber.Should().NotBeNullOrEmpty();
            vm.RequestNumber.Should().StartWith("RR-");
        }

        [Fact]
        public void Constructor_SetsDefaultRequestDate()
        {
            var vm = _services.GetService<NewRequestViewModel>();

            vm.RequestDate.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateRequestNumber_GeneratesNewNumber()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            var original = vm.RequestNumber;

            vm.GenerateRequestNumber();

            vm.RequestNumber.Should().NotBeNullOrEmpty();
            vm.RequestNumber.Should().StartWith("RR-");
        }

        [Fact]
        public void CanCreate_IsFalse_WhenRequestNumberIsEmpty()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "";
            vm.Title = "Test Title";

            vm.CanCreate.Should().BeFalse();
        }

        [Fact]
        public void CanCreate_IsFalse_WhenTitleIsEmpty()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "RR-123";
            vm.Title = "";

            vm.CanCreate.Should().BeFalse();
        }

        [Fact]
        public void CanCreate_IsTrue_WhenRequestNumberAndTitleAreSet()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "RR-123";
            vm.Title = "Test Title";

            vm.CanCreate.Should().BeTrue();
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsNull_WhenCanCreateIsFalse()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "";
            vm.Title = "";

            var result = await vm.CreateRequestAsync();

            result.Should().BeNull();
            _services.MockApi.Verify(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()), Times.Never);
        }

        [Fact]
        public async Task CreateRequestAsync_CallsApiService()
        {
            var expectedRequest = new RecordsRequest { Id = "req-new", Title = "Test" };
            _services.MockApi.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()))
                .ReturnsAsync(expectedRequest);

            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "RR-123";
            vm.Title = "Test";

            var result = await vm.CreateRequestAsync();

            result.Should().NotBeNull();
            result!.Id.Should().Be("req-new");
            _services.MockApi.Verify(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()), Times.Once);
        }

        [Fact]
        public async Task CreateRequestAsync_SetsIsLoading()
        {
            _services.MockApi.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()))
                .ReturnsAsync(new RecordsRequest { Id = "req-new" });

            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "RR-123";
            vm.Title = "Test";

            await vm.CreateRequestAsync();

            vm.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task CreateRequestAsync_IncludesNotes_WhenProvided()
        {
            CreateRequestPayload? capturedPayload = null;
            _services.MockApi.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()))
                .Callback<CreateRequestPayload>(p => capturedPayload = p)
                .ReturnsAsync(new RecordsRequest { Id = "req-new" });

            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "RR-123";
            vm.Title = "Test";
            vm.Notes = "Some notes";

            await vm.CreateRequestAsync();

            capturedPayload.Should().NotBeNull();
            capturedPayload!.Notes.Should().Be("Some notes");
        }

        [Fact]
        public async Task CreateRequestAsync_ExcludesNotes_WhenEmpty()
        {
            CreateRequestPayload? capturedPayload = null;
            _services.MockApi.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()))
                .Callback<CreateRequestPayload>(p => capturedPayload = p)
                .ReturnsAsync(new RecordsRequest { Id = "req-new" });

            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "RR-123";
            vm.Title = "Test";
            vm.Notes = "";

            await vm.CreateRequestAsync();

            capturedPayload.Should().NotBeNull();
            capturedPayload!.Notes.Should().BeNull();
        }

        [Fact]
        public async Task CreateRequestAsync_OnError_SetsErrorMessage()
        {
            _services.MockApi.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()))
                .ThrowsAsync(new Exception("API Error"));

            var vm = _services.GetService<NewRequestViewModel>();
            vm.RequestNumber = "RR-123";
            vm.Title = "Test";

            var result = await vm.CreateRequestAsync();

            result.Should().BeNull();
            vm.ErrorMessage.Should().NotBeNull();
            vm.ErrorMessage.Should().Contain("API Error");
        }

        [Fact]
        public void Title_Property_CanBeSetAndRetrieved()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            vm.Title = "New Title";

            vm.Title.Should().Be("New Title");
        }

        [Fact]
        public void Notes_Property_CanBeSetAndRetrieved()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            vm.Notes = "Some notes";

            vm.Notes.Should().Be("Some notes");
        }

        [Fact]
        public void RequestDate_Property_CanBeSetAndRetrieved()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            var date = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
            vm.RequestDate = date;

            vm.RequestDate.Should().Be(date);
        }

        [Fact]
        public void CanCreate_NotifiesPropertyChanged_WhenRequestNumberChanges()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            var propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.CanCreate))
                    propertyChanged = true;
            };

            vm.RequestNumber = "RR-NEW";

            propertyChanged.Should().BeTrue();
        }

        [Fact]
        public void CanCreate_NotifiesPropertyChanged_WhenTitleChanges()
        {
            var vm = _services.GetService<NewRequestViewModel>();
            var propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.CanCreate))
                    propertyChanged = true;
            };

            vm.Title = "New Title";

            propertyChanged.Should().BeTrue();
        }
    }
}
