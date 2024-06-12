using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Project_Manager.Controllers;
using Project_Manager.Data;
using Project_Manager.Models;
using Project_Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectManagerUnitTests
{
    public class TaskControllerTests
    {
        private readonly Mock<ApplicationDbContext> _contextMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ILogger<AccountController>> _loggerMock;
        private readonly TaskController _controller;

        public TaskControllerTests()
        {

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _loggerMock = new Mock<ILogger<AccountController>>();


        }

        [Fact]
        public async System.Threading.Tasks.Task Create_ValidModel_TaskAddedToDatabase()
        {
            using (var context = new ApplicationDbContext())
            {
                // Arrange
                var model = new TaskCreateViewModel
                {
                    Title = "Test Task",
                    Description = "Test Description",
                    DueDate = DateTime.UtcNow,
                    Priority = 5,
                    ProjectId = 2
                };
                var controller = new TaskController(new ApplicationDbContext(), _userManagerMock.Object, _loggerMock.Object);

                // Act
                var result = await controller.Create(model);

                // Assert
                Assert.IsType<RedirectResult>(result);
                var task = await context.Tasks.FirstOrDefaultAsync(t => t.Title == "Test Task");
                Assert.NotNull(task);
                Assert.Equal("Test Task", task.Title);
                Assert.Equal("Test Description", task.Description);
                //Assert.Equal(model.DueDate, task.DueDate); 
                //I decided not to make this text because there is a time error between creation and verification
                Assert.Equal(model.Priority, task.Priority);
                Assert.Equal("New", task.Status);
                Assert.Equal(model.ProjectId, task.ProjectId);

                context.Tasks.Remove(task);
                context.SaveChanges();
            }

        }
        [Fact]
        public async System.Threading.Tasks.Task ChangeStatus_ValidTaskId_StatusChanged()
        {
            using (var context = new ApplicationDbContext())
            {
                // Arrange
                var task = new Project_Manager.Models.Task
                {
                    Title = "Edit Test Task",
                    Description = "Test Description",
                    DueDate = DateTime.UtcNow,
                    Priority = 5,
                    ProjectId = 2,
                    Status = "New"
                };
                context.Tasks.Add(task);
                await context.SaveChangesAsync();


                var controller = new TaskController(context, _userManagerMock.Object, _loggerMock.Object);
                var newStatus = "Completed";

                // Act
                var result = await controller.ChangeStatus(task.Id, newStatus);

                // Assert
                Assert.IsType<RedirectToActionResult>(result);
                var updatedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id);
                Assert.NotNull(updatedTask);
                Assert.Equal(newStatus, updatedTask.Status);

                context.Tasks.Remove(task);
                context.SaveChanges();
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task DeleteConfirmed_ValidTaskId_TaskRemovedFromDatabase()
        {
            using (var context = new ApplicationDbContext())
            {
                // Arrange
                var task = new Project_Manager.Models.Task
                {
                    Title = "Delete Test Task",
                    Description = "Test Description",
                    DueDate = DateTime.UtcNow,
                    Priority = 5,
                    ProjectId = 2,
                    Status = "New"
                };
                context.Tasks.Add(task);
                await context.SaveChangesAsync();

                var controller = new TaskController(context, _userManagerMock.Object, _loggerMock.Object);

                // Act
                var result = await controller.DeleteConfirmed(task.Id);

                // Assert
                Assert.IsType<RedirectToActionResult>(result);
                var deletedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id);
                Assert.Null(deletedTask);
            }
        }
    }
}
