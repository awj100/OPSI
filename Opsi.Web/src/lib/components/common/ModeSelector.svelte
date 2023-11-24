<script>
    import { RadioButtonGroup, RadioButton } from "carbon-components-svelte";
  </script>
  
  <RadioButtonGroup
    orientation="vertical"
    legendText="User mode"
    name="plan-vertical"
    selected="admin"
    style="padding-top: 0.75rem;"
  >
    <RadioButton labelText="Administrator" value="admin" />
    <RadioButton labelText="Freelancer" value="freelancer" />
  </RadioButtonGroup>
  