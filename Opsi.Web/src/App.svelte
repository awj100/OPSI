<script lang="ts">
  import LayoutGrid, { Cell } from "@smui/layout-grid";
  import Header from "./lib/components/Header.svelte";
  import Projects from "./lib/components/admin/Projects.svelte"
</script>

<Header/>

<LayoutGrid>
  <Cell spanDevices={{ desktop: 6, tablet: 4, phone: 2 }}>
    <Projects/>
  </Cell>
</LayoutGrid>
