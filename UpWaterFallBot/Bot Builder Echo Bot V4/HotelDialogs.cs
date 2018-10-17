using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Bot_Builder_Echo_Bot_V4.Dialogs
{
    public class HotelDialogs : DialogSet
    {
        /// <summary>The ID of the top-level dialog.</summary>
        public const string MainMenu = "mainMenu";

        public HotelDialogs(IStatePropertyAccessor<DialogState> dialogStateAccessor)
            : base(dialogStateAccessor)
        {
            // Add the prompts.
            Add(new ChoicePrompt(Inputs.Choice));
            Add(new NumberPrompt<int>(Inputs.Number));
            Add(new ConfirmPrompt(Inputs.Confirm));

            // define and add waterfall dialogs (welcome)
            WaterfallStep[] welcomeDialogSteps = new WaterfallStep[]
            {
                MainDialogSteps.PresentMenuAsync,
                MainDialogSteps.ProcessInputAsync,
                MainDialogSteps.RepeatMenuAsync,
            };

            Add(new WaterfallDialog(MainMenu, welcomeDialogSteps));

            // define and add waterfall dialogs (order)
            WaterfallStep[] orderDinnerDialogSteps = new WaterfallStep[]
            {
                OrderDinnerSteps.StartFoodSelectionAsync,
                OrderDinnerSteps.GetRoomNumberAsync,
                OrderDinnerSteps.ProcessOrderAsync,
            };

            Add(new WaterfallDialog(Dialogs.OrderDinner, orderDinnerDialogSteps));

            // define and add waterfall dialogs (orderprompts)
            WaterfallStep[] orderPromptDialogSteps = new WaterfallStep[]
            {
                OrderPromptSteps.PromptForItemAsync,
                OrderPromptSteps.ProcessInputAsync,
            };

            Add(new WaterfallDialog(Dialogs.OrderPrompt, orderPromptDialogSteps));

            WaterfallStep[] reserveTableDialogSteps = new WaterfallStep[]
            {
                ReserveTableSteps.StubAsync,
            };

            Add(new WaterfallDialog(Dialogs.ReserveTable, reserveTableDialogSteps));

            /*WaterfallStep[] phoneChoiceDialogSteps = new WaterfallStep[]
            {
                PhoneChoicePromptSteps.PromptForPhoneAsync,
                PhoneChoicePromptSteps.ProcessInputAsync,
            };*/
            WaterfallStep[] phoneChoiceDialogSteps = new WaterfallStep[]
            {
                PhoneChoicePromptSteps.PromptForPhoneAsync,
                PhoneChoicePromptSteps.ConfirmPhoneAsync,
                PhoneChoicePromptSteps.ProcessInputAsync,
            };

            Add(new WaterfallDialog(Dialogs.PhonePrompt, phoneChoiceDialogSteps));
        }

        /// <summary>Contains the IDs for the other dialogs in the set.</summary>
        private static class Dialogs
        {
            public const string OrderDinner = "orderDinner";
            public const string OrderPrompt = "orderPrompt";
            public const string ReserveTable = "reserveTable";
            public const string PhonePrompt = "phonePrompt";
            public const string ConfirmPrompt = "confirmPrompt";
        }

        /// <summary>Contains the IDs for the prompts used by the dialogs.</summary>
        private static class Inputs //aka types of prompts
        {
            public const string Choice = "choicePrompt";
            public const string Number = "numberPrompt";
            public const string Confirm = "confirmPrompt";
        }

        /// <summary>Contains the keys used to manage dialog state.</summary>
        private static class Outputs
        {
            public const string OrderCart = "orderCart";
            public const string OrderTotal = "orderTotal";
            public const string RoomNumber = "roomNumber";
            public const string PhoneNumber = "phoneNumber";
        }

        private class WelcomeChoice
        {
            public string Description { get; set; }

            public string DialogName { get; set; }
        }

        private class MenuChoice
        {
            public const string Cancel = "Cancel Order";
            public const string Process = "Process Order";

            public string Name { get; set; }

            public double Price { get; set; }

            public string Description => double.IsNaN(Price) ? Name : $"{Name} - ${Price:0.00}";
        }

        private class PhoneChoice
        {
            public string Description { get; set; }
        };

        private static class Lists
        {
            // shit for welcome loop
            public static List<WelcomeChoice> WelcomeOptions { get; } = new List<WelcomeChoice>
            {
                new WelcomeChoice { Description = "Order dinner??", DialogName = Dialogs.OrderDinner },
                new WelcomeChoice { Description = "Reserve a table", DialogName = Dialogs.ReserveTable },
                new WelcomeChoice { Description = "Pick a phone number!", DialogName = Dialogs.PhonePrompt },
            };

            private static readonly List<string> _welcomeList = WelcomeOptions.Select(x => x.Description).ToList();

            public static IList<Choice> WelcomeChoices { get; } = ChoiceFactory.ToChoices(_welcomeList);

            public static Activity WelcomeReprompt
            {
                get
                {
                    var reprompt = MessageFactory.SuggestedActions(_welcomeList, "Please choose an option");
                    reprompt.AttachmentLayout = AttachmentLayoutTypes.List;
                    return reprompt as Activity;
                }
            }

            // Shit for Ordering food
            public static List<MenuChoice> MenuOptions { get; } = new List<MenuChoice>
            {
                new MenuChoice { Name = "Potato Salad", Price = 5.99 },
                new MenuChoice { Name = "Tuna Sandwich", Price = 6.89 },
                new MenuChoice { Name = "Clam Chowder", Price = 4.50 },
                new MenuChoice { Name = MenuChoice.Process, Price = double.NaN },
                new MenuChoice { Name = MenuChoice.Cancel, Price = double.NaN },
            };

            public static List<PhoneChoice> PhoneOptions { get; } = new List<PhoneChoice>
            {
                new PhoneChoice { Description = "867-5309" },
                new PhoneChoice { Description = "759-6594" },
                new PhoneChoice { Description = "242-5115" },
            };

            private static readonly List<string> _menuList = MenuOptions.Select(x => x.Description).ToList();

            private static readonly List<string> _phoneList = PhoneOptions.Select(x => x.Description).ToList();

            public static IList<Choice> MenuChoices { get; } = ChoiceFactory.ToChoices(_menuList);

            public static Activity MenuReprompt
            {
                get
                {
                    var reprompt = MessageFactory.SuggestedActions(_menuList, "Please choose a menu option");
                    reprompt.AttachmentLayout = AttachmentLayoutTypes.List;
                    return reprompt as Activity;
                }
            }

            public static IList<Choice> PhoneChoices { get; } = ChoiceFactory.ToChoices(_phoneList);

            public static Activity PhoneReprompt
            {
                get
                {
                    var reprompt = MessageFactory.SuggestedActions(_phoneList, "Please choose a diff option");
                    reprompt.AttachmentLayout = AttachmentLayoutTypes.List;
                    return reprompt as Activity;
                }
            }
        }

        // waterfall dialog steps for food ordering
        private static class MainDialogSteps
        {
            public static async Task<DialogTurnResult> PresentMenuAsync (
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                await stepContext.Context.SendActivityAsync(
                    "Welcome to Contoso Hotel and Resort.",
                    cancellationToken: cancellationToken);
                return await stepContext.PromptAsync(
                    Inputs.Choice,
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Please make a selection."),
                        RetryPrompt = Lists.WelcomeReprompt,
                        Choices = Lists.WelcomeChoices,
                    },
                    cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessInputAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                var choice = (FoundChoice)stepContext.Result;
                var dialogId = Lists.WelcomeOptions[choice.Index].DialogName;

                return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
            }

            public static async Task<DialogTurnResult> RepeatMenuAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                return await stepContext.ReplaceDialogAsync(MainMenu, null, cancellationToken);
            }
        }

        // contains guest's dinner order
        private class OrderCart : List<MenuChoice>
        {
            public OrderCart()
                : base() { }

            public OrderCart(OrderCart other)
                : base(other) { }
        }

        private static class OrderDinnerSteps
        {
            public static async Task<DialogTurnResult> StartFoodSelectionAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                await stepContext.Context.SendActivityAsync(
                    "Welcome to dinner service, yo.",
                    cancellationToken: cancellationToken);

                return await stepContext.BeginDialogAsync(Dialogs.OrderPrompt, null, cancellationToken);
            }

            public static async Task<DialogTurnResult> GetRoomNumberAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                if (stepContext.Result != null && stepContext.Result is OrderCart cart)
                {
                    stepContext.Values[Outputs.OrderCart] = cart;
                    return await stepContext.PromptAsync(
                        Inputs.Number,
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text("What is your room number?"),
                            RetryPrompt = MessageFactory.Text("Please enter your room number."),
                        },
                        cancellationToken);
                }
                else
                {
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }

            public static async Task<DialogTurnResult> ProcessOrderAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                var roomNumber = (int)stepContext.Result;
                stepContext.Values[Outputs.RoomNumber] = roomNumber;

                await stepContext.Context.SendActivityAsync(
                    $"Thank you!! Your order will be delivered to room {roomNumber} whenever we get around to it.",
                    cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private static class OrderPromptSteps
        {
            public static async Task<DialogTurnResult> PromptForItemAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                var cart = (stepContext.Options is OrderCart oldCart && oldCart != null)
                    ? new OrderCart(oldCart) : new OrderCart();

                stepContext.Values[Outputs.OrderCart] = cart;
                stepContext.Values[Outputs.OrderTotal] = cart.Sum(item => item.Price);

                return await stepContext.PromptAsync(
                    Inputs.Choice,
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("What would you like?"),
                        RetryPrompt = Lists.MenuReprompt,
                        Choices = Lists.MenuChoices,
                    },
                    cancellationToken);
            }


            public static async Task<DialogTurnResult> ProcessInputAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                var choice = (FoundChoice)stepContext.Result;
                var menuOption = Lists.MenuOptions[choice.Index];

                var cart = (OrderCart)stepContext.Values[Outputs.OrderCart];

                if (menuOption.Name is MenuChoice.Process)
                {
                    if (cart.Count > 0)
                    {
                        return await stepContext.EndDialogAsync(cart, cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(
                            "Your cart is empty, go away.",
                            cancellationToken: cancellationToken);
                        return await stepContext.ReplaceDialogAsync(Dialogs.OrderPrompt, null, cancellationToken);
                    }
                }
                else if (menuOption.Name is MenuChoice.Cancel)
                {
                    await stepContext.Context.SendActivityAsync(
                        "your order has been cancelled",
                        cancellationToken: cancellationToken);

                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
                else
                {
                    cart.Add(menuOption);
                    var total = (double)stepContext.Values[Outputs.OrderTotal] + menuOption.Price;
                    stepContext.Values[Outputs.OrderTotal] = total;

                    await stepContext.Context.SendActivityAsync(
                        $"Added {menuOption.Name} (${menuOption.Price:0.00}) to your order." +
                        Environment.NewLine + Environment.NewLine +
                        $"Your current total is ${total:0.00}.",
                        cancellationToken: cancellationToken);

                    return await stepContext.ReplaceDialogAsync(Dialogs.OrderPrompt, cart);
                }
            }
        }

        private static class PhoneChoicePromptSteps
        {
            public static async Task<DialogTurnResult> PromptForPhoneAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                return await stepContext.PromptAsync(
                    Inputs.Choice,
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Which phone number is yours??"),
                        RetryPrompt = Lists.PhoneReprompt,
                        Choices = Lists.PhoneChoices,
                    },
                    cancellationToken);
            }

            public static async Task<DialogTurnResult> ConfirmPhoneAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                var phoneNumber = stepContext.Context.Activity.Text;
                stepContext.Values[Outputs.PhoneNumber] = phoneNumber;

                return await stepContext.PromptAsync(
                    Inputs.Confirm,
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"Is {phoneNumber} your phone number?"),
                    },
                    cancellationToken);
            }

            public static async Task<DialogTurnResult> ProcessInputAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                if ((bool)stepContext.Result)
                {
                    await stepContext.Context.SendActivityAsync(
                        $"Calling {stepContext.Values[Outputs.PhoneNumber]}",
                        cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
                else
                {
                    return await stepContext.ReplaceDialogAsync(Dialogs.PhonePrompt, null, cancellationToken);
                } 
            }
        }

        private static class ReserveTableSteps
        {
            public static async Task<DialogTurnResult> StubAsync(
                WaterfallStepContext stepContext,
                CancellationToken cancellationToken)
            {
                await stepContext.Context.SendActivityAsync(
                    "Your table is down there.",
                    cancellationToken: cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}
