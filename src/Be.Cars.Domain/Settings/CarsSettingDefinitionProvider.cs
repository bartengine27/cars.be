using Volo.Abp.Settings;

namespace Be.Cars.Settings;

public class CarsSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(CarsSettings.MySetting1));
    }
}
