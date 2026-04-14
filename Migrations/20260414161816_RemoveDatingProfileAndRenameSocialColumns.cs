using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDatingProfileAndRenameSocialColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LookingFor",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "DiscoveryBoostCached",
                table: "Users",
                newName: "FeedBoostCached");

            migrationBuilder.RenameColumn(
                name: "UnlimitedLikes",
                table: "SubscriptionPlans",
                newName: "UnlimitedFollows");

            migrationBuilder.RenameColumn(
                name: "SeeWhoLikedYou",
                table: "SubscriptionPlans",
                newName: "SeeFollowersList");

            migrationBuilder.RenameColumn(
                name: "PriorityInDiscovery",
                table: "SubscriptionPlans",
                newName: "PriorityInFeed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FeedBoostCached",
                table: "Users",
                newName: "DiscoveryBoostCached");

            migrationBuilder.RenameColumn(
                name: "UnlimitedFollows",
                table: "SubscriptionPlans",
                newName: "UnlimitedLikes");

            migrationBuilder.RenameColumn(
                name: "SeeFollowersList",
                table: "SubscriptionPlans",
                newName: "SeeWhoLikedYou");

            migrationBuilder.RenameColumn(
                name: "PriorityInFeed",
                table: "SubscriptionPlans",
                newName: "PriorityInDiscovery");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LookingFor",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
