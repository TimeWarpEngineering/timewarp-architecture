import { Selector } from "testcafe";

fixture`WeatherForecastsPage`.page`https://localhost:5011/weatherforecasts`;

test("Should have data", async t => {
  const weatherForecastTableRows = Selector('[data-qa="WeatherForecastTable"] tr')
    .with({visibilityCheck: true});

  await t.expect(weatherForecastTableRows.count).gt(5);
});
