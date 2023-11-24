<script lang="ts">
	import { createEventDispatcher } from 'svelte';
  import { Row, Column } from "carbon-components-svelte";
  import { ProjectStates } from "../../enums/ProjectStates";
  import { Dropdown } from "carbon-components-svelte";
  import type { DropdownItem } from "carbon-components-svelte/types/Dropdown/Dropdown.svelte";

  let availableProjectStates: DropdownItem[] = [];
  for (let projectState in ProjectStates) {
    availableProjectStates.push({
      id: projectState,
      text: projectState
    });
  }

	const dispatch = createEventDispatcher();

  function onSelected(e: CustomEvent<{ selectedId: ProjectStates; selectedItem: DropdownItem; }>) {
    dispatch("selected", e.detail.selectedId);
  }

  export let projectState: ProjectStates = ProjectStates.InProgress;
</script>

<Row>
  <Column>
    <Dropdown
      id="admin.projectStateSelector"
      type="inline"
      titleText="Project state"
      selectedId={projectState}
      items={availableProjectStates}
      on:select={onSelected}
      />
  </Column>
</Row>