using Moq;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;
using ShoppingCart.Tests.Datasets;
using ShoppingCart.Web.Areas.Admin.Controllers;
using System.Linq.Expressions;
using Xunit;

namespace ShoppingCart.Tests
{
    public class CategoryControllerTests
    {
        private Mock<ICategoryRepository> _repositoryMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private CategoryController _controller;
        
        public CategoryControllerTests()
        {
            _repositoryMock = new Mock<ICategoryRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _controller = new CategoryController(_unitOfWorkMock.Object);
        }
        
        [Fact]
        public void GetCategories_All_ReturnAllCategories()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetAll(It.IsAny<string>()))
                .Returns(() => CategoryDataset.Categories);
            _unitOfWorkMock
                .Setup(uow => uow.Category)
                .Returns(_repositoryMock.Object);
            _controller = new CategoryController(_unitOfWorkMock.Object);
        
            // Act
            var result = _controller.Get();
        
            // Assert
            Assert.Equal(CategoryDataset.Categories, result.Categories);
        }
        
        [Fact]
        public void Get_ReturnsEmptyListIfNoCategories()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetAll(It.IsAny<string>()))
                .Returns(() => new List<Category>());
        
            _unitOfWorkMock
                .Setup(uow => uow.Category)
                .Returns(_repositoryMock.Object);
        
            // Act
            _controller = new CategoryController(_unitOfWorkMock.Object);
            var result = _controller.Get();
        
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Categories);
        }
        
        [Fact]
        public void GetCategoryById_ReturnsExpectedCategory()
        {
            // Arrange
            int categoryId = 1;
            var expectedCategory = new Category { Id = categoryId, Name = "NEW_CATEGORY" };
        
            _repositoryMock
                .Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null))
                .Returns(expectedCategory);
        
            _unitOfWorkMock
                .Setup(uow => uow.Category)
                .Returns(_repositoryMock.Object);
        
            // Act
            _controller = new CategoryController(_unitOfWorkMock.Object);
            var result = _controller.Get(categoryId);
        
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Categories);
            Assert.Equal(expectedCategory.Id, result.Category.Id);
            Assert.Equal(expectedCategory.Name, result.Category.Name);
        }
        
        [Fact]
        public void GetCategoryById_ThrowsExceptionIfCategoryNotFound()
        {
            // Arrange
            int categoryId = 1;
            var expectedCategory = new Category { Id = categoryId, Name = "NEW_CATEGORY" };
        
            _repositoryMock
                .Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null))
                .Returns(expectedCategory);
        
            _unitOfWorkMock
                .Setup(uow => uow.Category)
                .Returns(_repositoryMock.Object);
        
            // Act
            _controller = new CategoryController(_unitOfWorkMock.Object);
        
            // Assert
            Assert.Throws<Exception>(() => _controller.Get(2));
        }
        
        [Fact]
        public void CreateUpdate_AddsNewCategory_WhenCategoryIdIsZero()
        {
            // Arrange
            var categoryVM = new CategoryVM { Category = new Category { Id = 0, Name = "NewCategory" } };
            _repositoryMock
                .Setup(r => r.Add(categoryVM.Category));
        
            _unitOfWorkMock
                .Setup(uow => uow.Category)
                .Returns(_repositoryMock.Object);
            _controller = new CategoryController(_unitOfWorkMock.Object);
        
            // Act
            _controller.CreateUpdate(categoryVM);
        
            // Assert
            _unitOfWorkMock.Verify(uow => uow.Category.Add(categoryVM.Category), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.Category.Update(It.IsAny<Category>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.Save(), Times.Once);
        }
        
        
        [Fact]
        public void CreateUpdateCategory_InvalidModel_ThrowsSpecificException()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");
        
            // Act
            var exception = Assert
                .Throws<Exception>(() => _controller.CreateUpdate(new CategoryVM()));
        
            // Assert
            Assert
                .Equal("Model is invalid", exception.Message);
        }
        
        [Fact]
        public void DeleteCategory_ValidId_DeletesAndSaves()
        {
            // Arrange
            int categoryId = 1;
            var categoryToDelete = new Category { Id = categoryId, Name = "Category Name" };
        
            _repositoryMock
                .Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null))
                .Returns(categoryToDelete);
        
            _unitOfWorkMock
                .Setup(uow => uow.Category)
                .Returns(_repositoryMock.Object);
        
            _unitOfWorkMock
                .Setup(uow => uow.Save())
                .Verifiable();
        
            // Act
            _controller.DeleteData(categoryId);
        
            // Assert
            _repositoryMock.Verify(r => r.Delete(categoryToDelete), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.Save(), Times.Once);
        }
    }
}
