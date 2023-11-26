<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { Tooltip } from "carbon-components-svelte";

  export let icon: any;
  export let tooltip: string;
  let shouldShowTooltip: boolean = false;
  const dispatch = createEventDispatcher();

  function setTooltipVisibility(shouldShow: boolean) {
    shouldShowTooltip = shouldShow;
  }

  function onClick() {
    dispatch("click");
  }
</script>

<span class="project-action"
      role="button"
      tabindex="0"
      on:blur={() => setTooltipVisibility(false)}
      on:click={onClick}
      on:keypress={onClick}
      on:focus={() => setTooltipVisibility(true)}
      on:mouseout={() => setTooltipVisibility(false)}
      on:mouseover={() => setTooltipVisibility(true)}>
  <Tooltip
      direction="right"
      {icon}
      open={shouldShowTooltip}>
    <p>{tooltip}</p>
  </Tooltip>
</span>

<style lang="scss">
  .project-action {
    cursor: pointer;
    display: block;
    margin-bottom: 1rem;;
  }
</style>