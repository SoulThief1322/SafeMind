using NUnit.Framework;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class GoalServiceTests
    {
        [Test]
        public async Task GetOrCreateWeeklyGoals_NoExisting_CreatesNew()
        {
            using var context = TestDbContextFactory.Create();

            // Add some templates
            context.GoalTemplates.AddRange(
                new GoalTemplate { Description = "Goal 1" },
                new GoalTemplate { Description = "Goal 2" },
                new GoalTemplate { Description = "Goal 3" },
                new GoalTemplate { Description = "Goal 4" },
                new GoalTemplate { Description = "Goal 5" }
            );
            await context.SaveChangesAsync();

            var service = new GoalService(context);
            var goals = await service.GetOrCreateWeeklyGoalsAsync("user-1");

            Assert.That(goals.Count, Is.EqualTo(4));
            Assert.That(goals.All(g => g.UserId == "user-1"), Is.True);
            Assert.That(goals.All(g => !g.IsCompleted), Is.True);
        }

        [Test]
        public async Task GetOrCreateWeeklyGoals_ExistingGoals_ReturnsExisting()
        {
            using var context = TestDbContextFactory.Create();

            var template = new GoalTemplate { Description = "Existing Goal" };
            context.GoalTemplates.Add(template);
            await context.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;
            int diff = ((int)today.DayOfWeek + 6) % 7;
            var weekStart = today.AddDays(-diff);

            context.WeeklyGoals.Add(new WeeklyGoal
            {
                GoalTemplateId = template.Id, UserId = "user-2",
                WeekStart = weekStart, IsCompleted = false
            });
            await context.SaveChangesAsync();

            var service = new GoalService(context);
            var goals = await service.GetOrCreateWeeklyGoalsAsync("user-2");

            Assert.That(goals.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CompleteGoal_ValidGoal_Succeeds()
        {
            using var context = TestDbContextFactory.Create();

            var template = new GoalTemplate { Description = "Complete me" };
            context.GoalTemplates.Add(template);
            await context.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;
            int diff = ((int)today.DayOfWeek + 6) % 7;
            var weekStart = today.AddDays(-diff);

            var goal = new WeeklyGoal
            {
                GoalTemplateId = template.Id, UserId = "user-3",
                WeekStart = weekStart, IsCompleted = false
            };
            context.WeeklyGoals.Add(goal);
            await context.SaveChangesAsync();

            var service = new GoalService(context);
            var result = await service.CompleteGoalAsync("user-3", goal.Id);

            Assert.That(result, Is.True);
            var updated = await context.WeeklyGoals.FindAsync(goal.Id);
            Assert.That(updated!.IsCompleted, Is.True);
        }

        [Test]
        public async Task CompleteGoal_AlreadyCompleted_ReturnsFalse()
        {
            using var context = TestDbContextFactory.Create();

            var template = new GoalTemplate { Description = "Already done" };
            context.GoalTemplates.Add(template);
            await context.SaveChangesAsync();

            var goal = new WeeklyGoal
            {
                GoalTemplateId = template.Id, UserId = "user-4",
                WeekStart = DateTime.UtcNow.Date, IsCompleted = true
            };
            context.WeeklyGoals.Add(goal);
            await context.SaveChangesAsync();

            var service = new GoalService(context);
            var result = await service.CompleteGoalAsync("user-4", goal.Id);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompleteGoal_WrongUser_ReturnsFalse()
        {
            using var context = TestDbContextFactory.Create();

            var template = new GoalTemplate { Description = "Not yours" };
            context.GoalTemplates.Add(template);
            await context.SaveChangesAsync();

            var goal = new WeeklyGoal
            {
                GoalTemplateId = template.Id, UserId = "user-5",
                WeekStart = DateTime.UtcNow.Date, IsCompleted = false
            };
            context.WeeklyGoals.Add(goal);
            await context.SaveChangesAsync();

            var service = new GoalService(context);
            var result = await service.CompleteGoalAsync("wrong-user", goal.Id);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompleteGoal_NonExistentGoal_ReturnsFalse()
        {
            using var context = TestDbContextFactory.Create();
            var service = new GoalService(context);

            var result = await service.CompleteGoalAsync("user-1", 9999);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetTotalCompleted_ReturnsCorrectCount()
        {
            using var context = TestDbContextFactory.Create();

            var template = new GoalTemplate { Description = "Template" };
            context.GoalTemplates.Add(template);
            await context.SaveChangesAsync();

            context.WeeklyGoals.AddRange(
                new WeeklyGoal { GoalTemplateId = template.Id, UserId = "user-6", WeekStart = DateTime.UtcNow.Date, IsCompleted = true },
                new WeeklyGoal { GoalTemplateId = template.Id, UserId = "user-6", WeekStart = DateTime.UtcNow.Date.AddDays(-7), IsCompleted = true },
                new WeeklyGoal { GoalTemplateId = template.Id, UserId = "user-6", WeekStart = DateTime.UtcNow.Date.AddDays(-14), IsCompleted = false },
                new WeeklyGoal { GoalTemplateId = template.Id, UserId = "other-user", WeekStart = DateTime.UtcNow.Date, IsCompleted = true }
            );
            await context.SaveChangesAsync();

            var service = new GoalService(context);
            var count = await service.GetTotalCompletedAsync("user-6");

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetTotalCompleted_NoGoals_ReturnsZero()
        {
            using var context = TestDbContextFactory.Create();
            var service = new GoalService(context);

            var count = await service.GetTotalCompletedAsync("no-goals-user");

            Assert.That(count, Is.EqualTo(0));
        }
    }
}
