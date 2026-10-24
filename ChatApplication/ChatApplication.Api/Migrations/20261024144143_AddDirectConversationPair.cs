using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApplication.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectConversationPair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DirectConversationUser1Id",
                table: "Conversation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DirectConversationUser2Id",
                table: "Conversation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_DirectConversationUser1Id_DirectConversationUser2Id",
                table: "Conversation",
                columns: new[] { "DirectConversationUser1Id", "DirectConversationUser2Id" },
                unique: true,
                filter: "[ConversationType] = N'Direct' AND [DirectConversationUser1Id] IS NOT NULL AND [DirectConversationUser2Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_DirectConversationUser2Id",
                table: "Conversation",
                column: "DirectConversationUser2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversation_User_DirectConversationUser1Id",
                table: "Conversation",
                column: "DirectConversationUser1Id",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversation_User_DirectConversationUser2Id",
                table: "Conversation",
                column: "DirectConversationUser2Id",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversation_User_DirectConversationUser1Id",
                table: "Conversation");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversation_User_DirectConversationUser2Id",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_DirectConversationUser1Id_DirectConversationUser2Id",
                table: "Conversation");

            migrationBuilder.DropIndex(
                name: "IX_Conversation_DirectConversationUser2Id",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "DirectConversationUser1Id",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "DirectConversationUser2Id",
                table: "Conversation");
        }
    }
}
