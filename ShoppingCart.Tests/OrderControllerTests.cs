using Moq;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;
using ShoppingCart.Web.Areas.Admin.Controllers;
using System.Linq.Expressions;
using Xunit;
using ShoppingCart.Utility;
using Stripe;


namespace ShoppingCart.Tests
{
    public class OrderControllerTests
    {
        [Fact]
        public void OrderDetails_Returns_OrderVM_With_OrderHeader_And_OrderDetails()
        {
            // Arrange
            int orderId = 1;
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var orderHeader = new OrderHeader { Id = orderId };
            var orderDetails = new List<OrderDetail> { new OrderDetail { Id = 1 }, new OrderDetail { Id = 2 } };
            mockUnitOfWork
                .Setup(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), It.IsAny<string>()))
                .Returns(orderHeader);
            mockUnitOfWork
                .Setup(uow => uow.OrderDetail.GetAll(It.IsAny<string>()))
                .Returns(orderDetails);
            var controller = new OrderController(mockUnitOfWork.Object);

            // Act
            var result = controller.OrderDetails(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.OrderHeader);
            Assert.Equal(orderId, result.OrderHeader.Id);
            Assert.NotNull(result.OrderDetails);
            Assert.Equal(orderDetails.Count, result.OrderDetails.Count());
        }

        [Fact]
        public void SetToInProcess_Updates_OrderStatus_To_InProcess()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var orderVM = new OrderVM { OrderHeader = new OrderHeader { Id = 1, OrderStatus = OrderStatus.StatusPending } };
            var controller = new OrderController(mockUnitOfWork.Object);

            // Act
            controller.SetToInProcess(orderVM);

            // Assert
            mockUnitOfWork.Verify(uow => uow.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, OrderStatus.StatusInProcess, It.IsAny<string>()), Times.Once);
            mockUnitOfWork.Verify(uow => uow.Save(), Times.Once);
        }

        [Fact]
        public void SetToShipped_Updates_OrderHeader_With_ShippingInfo_And_Status_To_Shipped()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var orderVM = new OrderVM
            {
                OrderHeader = new OrderHeader { Id = 1, Carrier = "UPS", TrackingNumber = "123456", OrderStatus = OrderStatus.StatusInProcess }
            };
            var controller = new OrderController(mockUnitOfWork.Object);

            // Act
            controller.SetToShipped(orderVM);

            // Assert
            mockUnitOfWork.Verify(uow => uow.OrderHeader.Update(It.IsAny<OrderHeader>()), Times.Once);
            mockUnitOfWork.Verify(uow => uow.Save(), Times.Once);
        }

        [Fact]
        public void SetToCancelOrder_Refunds_If_PaymentStatus_Is_Approved()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var orderHeader = new OrderHeader { Id = 1, PaymentStatus = PaymentStatus.StatusApproved, PaymentIntentId = "payment_intent_id" };
            var orderVM = new OrderVM { OrderHeader = orderHeader };
            var refundServiceMock = new Mock<Stripe.RefundService>();
            refundServiceMock
                .Setup(service => service.Create(It.IsAny<RefundCreateOptions>(), It.IsAny<RequestOptions>()))
                .Returns(new Refund());
            var controller = new OrderController(mockUnitOfWork.Object);

            // Act
            controller.SetToCancelOrder(orderVM);

            // Assert
            refundServiceMock.Verify(service => service.Create(It.IsAny<RefundCreateOptions>(), It.IsAny<RequestOptions>()), Times.Once);
            mockUnitOfWork.Verify(uow => uow.OrderHeader.UpdateStatus(orderHeader.Id, OrderStatus.StatusCancelled, It.IsAny<string>()), Times.Once);
            mockUnitOfWork.Verify(uow => uow.Save(), Times.Once);
        }

        [Fact]
        public void SetToCancelOrder_Does_Not_Refund_If_PaymentStatus_Is_Not_Approved()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var orderHeader = new OrderHeader { Id = 1, PaymentStatus = PaymentStatus.StatusPending };
            var orderVM = new OrderVM { OrderHeader = orderHeader };
            var refundServiceMock = new Mock<Stripe.RefundService>();
            var controller = new OrderController(mockUnitOfWork.Object);

            // Act
            controller.SetToCancelOrder(orderVM);

            // Assert
            mockUnitOfWork.Verify(uow => uow.OrderHeader.UpdateStatus(orderHeader.Id, OrderStatus.StatusCancelled, It.IsAny<string>()), Times.Once);
            mockUnitOfWork.Verify(uow => uow.Save(), Times.Once);
        }
    }
}
