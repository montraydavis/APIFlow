﻿using APIFlow.Endpoint;
using APIFlow.Models;
using APIFlow.Tests.Fixtures;
using System.Web.Http;

namespace APIFlow.Tests.Contexts
{
    public class UserInformationContext : ApiContext<UserInformation>
    {
        public override bool HasBody => true;

        public override void ConfigureEndpoint(ref string endpoint, EndpointInputModel inputModel, bool randomizedInput = false)
        {
            base.ConfigureEndpoint(ref endpoint, inputModel, randomizedInput);

            base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value?[0].Id ?? 0);
        }

        /// <summary>
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="inputModel">Input Model.</param>
        [HttpGet()]
        [Route("https://aef3c493-6ff3-47a2-be7f-150688405f7e.mock.pstmn.io/UserInformation")]
        public override void ApplyContext(EndpointInputModel inputModel)
        {
            base.ConfigureModel<UserContext>((u, o) => o.Id = u.Value?[0].Id ?? 0);
            base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value?[0].Id ?? 0);
        }

        public UserInformationContext(UserInformation baseObject, EndpointInputModel inputModel) : base(baseObject, inputModel)
        {

        }
    }
}