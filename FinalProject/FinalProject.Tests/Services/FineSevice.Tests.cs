using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Application.Dto.Fine;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Services;
using FinalProject.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinalProject.Tests.Services
{
	public class FineServiceTests
	{
		private readonly Mock<IFineRepository> _fineRepositoryMock;
		private readonly Mock<IBorrowingTransactionRepository> _borrowingRepositoryMock;
		private readonly Mock<INotificationService> _notificationServiceMock;
		private readonly FineService _fineService;

		public FineServiceTests()
		{
			_fineRepositoryMock = new Mock<IFineRepository>();
			_borrowingRepositoryMock = new Mock<IBorrowingTransactionRepository>();
			_notificationServiceMock = new Mock<INotificationService>();

			_fineService = new FineService(
				_fineRepositoryMock.Object,
				_borrowingRepositoryMock.Object,
				_notificationServiceMock.Object
			);
		}

		[Fact]
		public async Task GetAllFinesAsync_ShouldReturnAllFines()
		{
			// Arrange
			var expectedFines = new List<Fine>
			{
				new Fine { FineID = 1, MemberID = 1, Amount = 50.00M, Status = "Pending", TransactionDate = DateTime.Now.AddDays(-5) },
				new Fine { FineID = 2, MemberID = 2, Amount = 25.00M, Status = "Paid", TransactionDate = DateTime.Now.AddDays(-10) }
			};

			_fineRepositoryMock.Setup(repo => repo.GetAllFinesAsync()).ReturnsAsync(expectedFines);

			// Act
			var result = await _fineService.GetAllFinesAsync();

			// Assert
			result.Should().BeEquivalentTo(expectedFines);
			_fineRepositoryMock.Verify(repo => repo.GetAllFinesAsync(), Times.Once);
		}

		[Fact]
		public async Task GetFineByIdAsync_ShouldReturnFine_WhenFineExists()
		{
			// Arrange
			var expectedFine = new Fine { FineID = 1, MemberID = 1, Amount = 50.00M, Status = "Pending", TransactionDate = DateTime.Now.AddDays(-5) };
			_fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(1)).ReturnsAsync(expectedFine);

			// Act
			var result = await _fineService.GetFineByIdAsync(1);

			// Assert
			result.Should().BeEquivalentTo(expectedFine);
			_fineRepositoryMock.Verify(repo => repo.GetFineByIdAsync(1), Times.Once);
		}

		[Fact]
		public async Task GetFinesForMemberAsync_ShouldReturnMemberFines()
		{
			// Arrange
			var memberId = 1;
			var expectedFines = new List<Fine>
			{
				new Fine { FineID = 1, MemberID = memberId, Amount = 50.00M, Status = "Pending", TransactionDate = DateTime.Now.AddDays(-5) }
			};

			_fineRepositoryMock.Setup(repo => repo.GetFinesForMemberAsync(memberId)).ReturnsAsync(expectedFines);

			// Act
			var result = await _fineService.GetFinesForMemberAsync(memberId);

			// Assert
			result.Should().BeEquivalentTo(expectedFines);
			_fineRepositoryMock.Verify(repo => repo.GetFinesForMemberAsync(memberId), Times.Once);
		}

		[Fact]
		public async Task GetFinesByMemberNameAsync_ShouldReturnFinesForMemberWithName()
		{
			// Arrange
			var memberName = "John";
			var expectedFines = new List<Fine>
			{
				new Fine { FineID = 1, MemberID = 1, Amount = 50.00M, Status = "Pending", TransactionDate = DateTime.Now.AddDays(-5) }
			};
			var expectedDtos = expectedFines.Select(f => new FineDetailsDto
			{
				FineID = f.FineID,
				MemberID = f.MemberID,
				Amount = f.Amount,
				Status = f.Status,
				TransactionDate = f.TransactionDate
			});

			_fineRepositoryMock.Setup(repo => repo.GetFinesByMemberNameAsync(memberName)).ReturnsAsync(expectedFines);

			// Act
			var result = await _fineService.GetFinesByMemberNameAsync(memberName);

			// Assert
			result.Should().BeEquivalentTo(expectedDtos);
			_fineRepositoryMock.Verify(repo => repo.GetFinesByMemberNameAsync(memberName), Times.Once);
		}

		[Fact]
		public async Task AddFineAsync_ShouldUpdateExistingFine_WhenFineExists()
		{
			// Arrange
			var memberId = 1;
			var createDto = new CreateFineDto
			{
				MemberID = memberId,
				Amount = 25.00M,
				Status = "Pending",
				TransactionDate = DateTime.Now
			};

			var existingFines = new List<Fine>
			{
				new Fine { FineID = 1, MemberID = memberId, Amount = 50.00M, Status = "Pending", TransactionDate = DateTime.Now.AddDays(-5) }
			};

			_fineRepositoryMock.Setup(repo => repo.GetFinesForMemberAsync(memberId)).ReturnsAsync(existingFines);

			// Act
			await _fineService.AddFineAsync(memberId, createDto);

			// Assert
			_fineRepositoryMock.Verify(repo => repo.UpdateFineAsync(It.Is<Fine>(f =>
				f.Amount == 75.00M && f.Status == createDto.Status)), Times.Once);
		}

		[Fact]
		public async Task AddFineAsync_ShouldAddNewFine_WhenNoFineExists()
		{
			// Arrange
			var memberId = 1;
			var createDto = new CreateFineDto
			{
				MemberID = memberId,
				Amount = 25.00M,
				Status = "Pending",
				TransactionDate = DateTime.Now
			};

			_fineRepositoryMock.Setup(repo => repo.GetFinesForMemberAsync(memberId)).ReturnsAsync(new List<Fine>());

			// Act
			await _fineService.AddFineAsync(memberId, createDto);

			// Assert
			_fineRepositoryMock.Verify(repo => repo.AddFineAsync(It.Is<Fine>(f =>
				f.MemberID == memberId && f.Amount == 25.00M)), Times.Once);
		}

		[Fact]
		public async Task UpdateFineForMemberAsync_ShouldUpdateFine()
		{
			// Arrange
			var memberId = 1;
			var updateDto = new UpdateFineDto
			{
				FineID = 1,
				MemberID = memberId,
				Amount = 75.00M,
				Status = "Pending",
				TransactionDate = DateTime.Now
			};

			// Act
			await _fineService.UpdateFineForMemberAsync(memberId, updateDto);

			// Assert
			_fineRepositoryMock.Verify(repo => repo.UpdateFineAsync(It.Is<Fine>(f =>
				f.FineID == updateDto.FineID &&
				f.MemberID == memberId &&
				f.Amount == Math.Min(updateDto.Amount, 300) &&
				f.Status == updateDto.Status)), Times.Once);
		}

		[Fact]
		public async Task UpdateFineByIdAsync_ShouldUpdateFine()
		{
			// Arrange
			var updateDto = new UpdateFineDto
			{
				FineID = 1,
				MemberID = 1,
				Amount = 75.00M,
				Status = "Pending",
				TransactionDate = DateTime.Now
			};

			// Act
			await _fineService.UpdateFineByIdAsync(updateDto);

			// Assert
			_fineRepositoryMock.Verify(repo => repo.UpdateFineAsync(It.Is<Fine>(f =>
				f.FineID == updateDto.FineID &&
				f.MemberID == updateDto.MemberID &&
				f.Amount == Math.Min(updateDto.Amount, 300) &&
				f.Status == updateDto.Status)), Times.Once);
		}

		[Fact]
		public async Task DeleteFineByIdAsync_ShouldDeleteFine_WhenStatusIsPaid()
		{
			// Arrange
			int fineId = 1;
			var fine = new Fine { FineID = fineId, Status = "Paid" };
			_fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(fineId)).ReturnsAsync(fine);

			// Act
			await _fineService.DeleteFineByIdAsync(fineId);

			// Assert
			_fineRepositoryMock.Verify(repo => repo.DeleteFineAsync(fineId), Times.Once);
		}

		[Fact]
		public async Task DeleteFineByIdAsync_ShouldThrowException_WhenStatusIsNotPaid()
		{
			// Arrange
			int fineId = 1;
			var fine = new Fine { FineID = fineId, Status = "Pending" };
			_fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(fineId)).ReturnsAsync(fine);

			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
				await _fineService.DeleteFineByIdAsync(fineId));
			_fineRepositoryMock.Verify(repo => repo.DeleteFineAsync(fineId), Times.Never);
		}

		[Fact]
		public async Task DetectAndApplyOverdueFinesAsync_ShouldApplyFines()
		{
			// Arrange
			var overdueTransactions = new List<BorrowingTransaction>
			{
				new BorrowingTransaction
				{
					TransactionID = 1, MemberID = 1,
					ReturnDate = DateTime.Now.AddDays(-5), // 5 days overdue
                    Member = new Member { MemberID = 1, Name = "John Doe" }
				},
				new BorrowingTransaction
				{
					TransactionID = 2, MemberID = 2,
					ReturnDate = DateTime.Now.AddDays(-35), // 35 days overdue (triggers suspension)
                    Member = new Member { MemberID = 2, Name = "Jane Smith" }
				}
			};

			_borrowingRepositoryMock.Setup(repo => repo.GetOverdueBooksAsync()).ReturnsAsync(overdueTransactions);

			// Act
			await _fineService.DetectAndApplyOverdueFinesAsync();

			// Assert
			// Verify that fines were added for both transactions
			_fineRepositoryMock.Verify(repo => repo.AddFineAsync(It.IsAny<Fine>()), Times.Exactly(2));

			// Verify that member 2 has suspension due to being over 30 days overdue
			_fineRepositoryMock.Verify(repo => repo.AddFineAsync(It.Is<Fine>(f =>
				f.MemberID == 2 && f.Amount > 300)), Times.Once); // Amount should include extra penalty
		}

		[Fact]
		public async Task PayFineAsync_ShouldMarkFineAsPaid_WhenPaymentAmountMatches()
		{
			// Arrange
			var payFineDto = new PayFineDto { FineID = 1, Amount = 50.00M };
			var fine = new Fine { FineID = 1, Amount = 50.00M, Status = "Pending" };

			_fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(payFineDto.FineID)).ReturnsAsync(fine);

			// Act
			await _fineService.PayFineAsync(payFineDto);

			// Assert
			_fineRepositoryMock.Verify(repo => repo.UpdateFineAsync(It.Is<Fine>(f =>
				f.FineID == payFineDto.FineID && f.Status == "Paid")), Times.Once);
			_notificationServiceMock.Verify(service => service.NotifyForFinePaymentAsync(payFineDto.FineID), Times.Once);
		}

		[Fact]
		public async Task PayFineAsync_ShouldThrowException_WhenFineIsAlreadyPaid()
		{
			// Arrange
			var payFineDto = new PayFineDto { FineID = 1, Amount = 50.00M };
			var fine = new Fine { FineID = 1, Amount = 50.00M, Status = "Paid" };

			_fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(payFineDto.FineID)).ReturnsAsync(fine);

			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
				await _fineService.PayFineAsync(payFineDto));
			_fineRepositoryMock.Verify(repo => repo.UpdateFineAsync(It.IsAny<Fine>()), Times.Never);
		}

		[Fact]
		public async Task PayFineAsync_ShouldThrowException_WhenPaymentAmountDoesNotMatch()
		{
			// Arrange
			var payFineDto = new PayFineDto { FineID = 1, Amount = 25.00M };
			var fine = new Fine { FineID = 1, Amount = 50.00M, Status = "Pending" };

			_fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(payFineDto.FineID)).ReturnsAsync(fine);

			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
				await _fineService.PayFineAsync(payFineDto));
			_fineRepositoryMock.Verify(repo => repo.UpdateFineAsync(It.IsAny<Fine>()), Times.Never);
		}
	}
}
