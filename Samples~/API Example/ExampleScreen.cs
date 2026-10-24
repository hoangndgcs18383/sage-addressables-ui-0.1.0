using SAGE.AddressablesUI;

namespace SAGE.AddressablesUI.Samples
{
    public sealed class ExampleScreenData : IUIData
    {
        public string Message;
    }

    // Attach this to the ROOT of a UI prefab and mark it Addressable as "ExampleScreen".
    public sealed class ExampleScreen : BaseScreen
    {
        public override void SetData(IUIData data = null)
        {
            if (data is ExampleScreenData example)
                UnityEngine.Debug.Log(example.Message);
        }
    }
}
