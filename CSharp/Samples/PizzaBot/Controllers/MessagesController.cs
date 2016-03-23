﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Form;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Bot.Sample.PizzaBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private static readonly Lazy<IForm<PizzaOrder>> PizzaForm = new Lazy<IForm<PizzaOrder>>
            (
            () =>
            {
                IForm<PizzaOrder> form = new Form<PizzaOrder>("PizzaForm");

                const bool NoNumbers = false;
                if (NoNumbers)
                {
                    form.Configuration().DefaultPrompt.ChoiceFormat = "{1}";
                }
                else
                {
                    form.Configuration().DefaultPrompt.ChoiceFormat = "{0}. {1}";
                }

                ConditionalDelegate<PizzaOrder> isBYO = (pizza) => pizza.Kind == PizzaOptions.BYOPizza;
                ConditionalDelegate<PizzaOrder> isSignature = (pizza) => pizza.Kind == PizzaOptions.SignaturePizza;
                ConditionalDelegate<PizzaOrder> isGourmet = (pizza) => pizza.Kind == PizzaOptions.GourmetDelitePizza;
                ConditionalDelegate<PizzaOrder> isStuffed = (pizza) => pizza.Kind == PizzaOptions.StuffedPizza;

                return form
                    // .Field(nameof(PizzaOrder.Choice))
                    .Field(nameof(PizzaOrder.Size))
                    .Field(nameof(PizzaOrder.Kind))
                    .Field("BYO.Crust", isBYO)
                    .Field("BYO.Sauce", isBYO)
                    .Field("BYO.Toppings", isBYO)
                    .Field(nameof(PizzaOrder.GourmetDelite), isGourmet)
                    .Field(nameof(PizzaOrder.Signature), isSignature)
                    .Field(nameof(PizzaOrder.Stuffed), isStuffed)
                    .AddRemainingFields()
                    .Confirm("Would you like a {Size}, {BYO.Crust} crust, {BYO.Sauce}, {BYO.Toppings} pizza?", isBYO)
                    .Confirm("Would you like a {Size}, {&Signature} {Signature} pizza?", isSignature, dependencies: new string[] { "Size", "Kind", "Signature" })
                    .Confirm("Would you like a {Size}, {&GourmetDelite} {GourmetDelite} pizza?", isGourmet)
                    .Confirm("Would you like a {Size}, {&Stuffed} {Stuffed} pizza?", isStuffed)
                    ;

            }, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and reply to it
        /// </summary>
        [ResponseType(typeof(Message))]
        public async Task<HttpResponseMessage> Post([FromBody]Message message)
        {
            var pizzaForm = PizzaForm.Value;
            var pizzaOrderDialog = new PizzaOrderDialog(pizzaForm);
            var dialogs = new DialogCollection().Add(pizzaForm).Add(pizzaOrderDialog);
            return await ConnectorSession.MessageReceivedAsync(Request, message, dialogs, pizzaOrderDialog);
        }
    }
}