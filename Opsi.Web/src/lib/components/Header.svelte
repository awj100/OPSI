<script lang="ts">
  import TopAppBar, {
    Row,
    Section,
    Title
  } from '@smui/top-app-bar';

  let topAppBar: TopAppBar;
</script>

<TopAppBar bind:this={topAppBar} variant="fixed" style="background-color: transparent; color: #c00">
  <Row>
    <Section>
      <Title>OPSI.Web</Title>
    </Section>
  </Row>
</TopAppBar>

<style>
  /* Hide everything above this component. */
  :global(#smui-app),
  :global(body),
  :global(html) {
    display: block !important;
    height: auto !important;
    width: auto !important;
    position: static !important;
  }
</style>