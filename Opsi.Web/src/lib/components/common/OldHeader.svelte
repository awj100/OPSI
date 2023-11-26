<script lang="ts">
  import { Column, Row, Theme } from "carbon-components-svelte";
</script>

<Row>
  <Column sm={4} md={7}>
    <header>
      <h1>OPSI.Web</h1>
    </header>
  </Column>
  <Column sm={4} md={1}>
    <Theme persist={true} render="select" />
  </Column>
</Row>