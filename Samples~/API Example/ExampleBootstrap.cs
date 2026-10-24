using System;
using UnityEngine;

namespace SAGE.AddressablesUI.Samples
{
    public sealed class ExampleBootstrap : MonoBehaviour
    {
        private async void Start()
        {
            try
            {
                var ui = UIManager.Create();
                await ui.ShowAndLoadScreenAsync<ExampleScreen>("ExampleScreen",
                    new ExampleScreenData { Message = "Screen loaded from Addressables" });
            }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
