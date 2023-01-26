# APIFlow

The ultimate solution for quick API Integration testing.

This library allows developers and test engineers to quickly build API Integration tests without having to repeatedly wire up endpoints. Instead, it utilizes a simple context configuration to specify inputs and automatically forward-feeds inputs from previous requests.

(It feels very similar to Entity Framework -- but for APIs)

# Forward-Feeding Endpoints

The main purpose of this library is to allow sequential API execution and testing without the necessity of re-writing code to wire up ModelA to ModelB.

This forward-feeding implementation is simple, yet creating complete end-to-end workflows can be done very quickly since wiring up of models only needs to happen once.

## Scenario

You have two endpoints.

1. Fetch Users /api/Users
1. Fetch User Account Information by Id /api/Users?Id={Users.id}

Traditionally, if you had to execute these endpoints in succession, you would need to

1. Create each model.
1. Make Http Requests.
1. Wire up inputs for next endpoint payload.
1. Repeat for remaining endpoints.

With APIFlow, step 2 & 3 is removed as the forward-feed capabilities is automatic with a one-time configuration setup.

# Example Context

```

public class UserContext : ApiContext<List<User>>
{
    [HttpGet()]
    [Route("https://aef3c493-6ff3-47a2-be7f-150688405f7e.mock.pstmn.io/Users")]
    public override void ApplyContext(EndpointInputModel model)
    {

    }

    public override void ConfigureClient(ref HttpClientWrapper wrapper)
    {
        base.ConfigureClient(ref wrapper);

        // Add necessary headers etc.
    }

    public UserContext(List<User> baseObject,
        EndpointInputModel inputModel) : base(baseObject, inputModel)
    {

    }
}

public class UserInformationContext : ApiContext<UserInformation>
{
    public override void ConfigureEndpoint(ref string endpoint, EndpointInputModel inputModel, bool randomizedInput = false)
    {
        base.ConfigureEndpoint(ref endpoint, inputModel, randomizedInput);

        base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value[0].Id);
    }
    
    [HttpGet()]
    [Route("https://aef3c493-6ff3-47a2-be7f-150688405f7e.mock.pstmn.io/UserInformation")]
    public override void ApplyContext(EndpointInputModel inputModel)
    {
        base.ConfigureModel<UserContext>((u, o) => o.Id = u.Value[0].Id);
        base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value[0].Id);
    }

    public UserInformationContext(UserInformation baseObject, EndpointInputModel inputModel) : base(baseObject, inputModel)
    {

    }
}

```
