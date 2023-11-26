<script lang="ts">
  import { createEventDispatcher, tick } from "svelte";
  import { Form, FormGroup, Modal, TextInput } from "carbon-components-svelte";
  import RootNode from "./TreeView/RootNode.svelte";
  import UserAssignment from "./UserAssignment";
  import type Node from "./TreeView/Node";
  import ResourceModel from "../../../Models/Resource";
  import { assignUser } from "../../../services/assignmentsService";
  import AssignmentError from "./AssignmentError";
  import axios from "axios";
  import AssignmentErrors from "./AssignmentErrors.svelte";

  const defaultFileStatus = "inactive";
  const dispatch = createEventDispatcher();
  const idElmntUsername = "elntUsernameForAssignment";
  let assignmentErrors: AssignmentError[] = [];
  let hasFormSubmitted: boolean = false;
  let isUsernameInvalid: boolean = false;
  let username: string = "";
  export let openModal: boolean = false;
  export let projectId: string;
  export let selectedResources: ResourceModel[];

  $: isUsernameInvalid = hasFormSubmitted && username.length === 0;
  $: resourceNodes = getStructure(selectedResources);

  function getStructure(resources: ResourceModel[]): Node[] {
    const result: Array<any> = [];
    const level = { result };
    const paths = resources.map((resource: ResourceModel) => resource.fullName!);
    const pathSeparator = "/";
    
    function reducer(accumulator: any, currentValue: any, idx: number, arr: Array<any>) {
      if (accumulator[currentValue]) {
          return accumulator[currentValue];
      }
  
      accumulator[currentValue] = {
          result: []
      };
  
      const node = {
          children: [],
          status: defaultFileStatus,
          text: currentValue
      };
  
      if (idx < arr.length - 1) { // If this is a branch, not the leaf...
        node.children = accumulator[currentValue].result;
      }

      accumulator.result.push(node);    
      return accumulator[currentValue];
    }

    paths.forEach(path => {
        path.split(pathSeparator).reduce(reducer, level)
    });

    return result;
  }

  async function onSubmit(e: CustomEvent<null>) {
    hasFormSubmitted = true;

    // Let isUsernameInvalid be updated.
    await tick();

    if (!isUsernameInvalid) {

      await assignUserToResources();

      // Let the parent know that user assignment is successful.
      dispatch("userAssigned", new UserAssignment(username, selectedResources));

      // Reset things.
      hasFormSubmitted = false;
      username = "";
    }
  }

  async function assignUserToResource(resource: ResourceModel) {
    const node = getResourceNode(resource.fullName!);
    if (node !== undefined) {
      node.status = "active";
    }

    resourceNodes = [...resourceNodes];
    try {
      const response = await assignUser(username, projectId, resource);

      if (response.status === 202 && node !== undefined) {
        node.status = "finished";
      }
    } catch (error) {
      const errorMessage = axios.isAxiosError(error) ? error.response!.data : (error as any).toString();
      assignmentErrors.push(new AssignmentError(resource.fullName!, errorMessage));
      if (node !== undefined) {
        node.status = "error";
      }
      assignmentErrors = [...assignmentErrors];
    }
    
    resourceNodes = [...resourceNodes];
  }

  async function assignUserToResources() {
    for (let i = 0; i < selectedResources.length; i++) {
      await assignUserToResource(selectedResources[i]);
    }

    if (assignmentErrors.length === 0) {
      openModal = false;
    }
  }

  function getResourceNode(resourceFullName: string): Node | undefined {
    const pathSeparator: string = "/";

    function getNodeFromNextLevel(nextLevelNodes: Node[], currentPath: string): Node | undefined {
      const pathSeparatorForThisLevel = currentPath.length > 0 ? pathSeparator : "";

      for (let i = 0; i < nextLevelNodes.length; i++) {
        const thisNode = nextLevelNodes[i];
        const nextLevelPath = `${currentPath}${pathSeparatorForThisLevel}${thisNode.text}`;
        if (!resourceFullName.startsWith(nextLevelPath)) {
          continue;
        }

        if (thisNode.children.length === 0) {
          return thisNode;
        }

        return getNodeFromNextLevel(thisNode.children, nextLevelPath);
      }

      return undefined;
    }

    return getNodeFromNextLevel(resourceNodes, "");
  }
</script>

<Modal
  hasForm={true}
  hasScrollingContent={true}
  shouldSubmitOnEnter={false}
  bind:open={openModal}
  modalHeading="User Assignment"
  primaryButtonText="OK"
  secondaryButtonText="Cancel"
  on:click:button--secondary={() => (openModal = false)}
  on:open
  on:close
  on:submit={onSubmit}
  class="user-assignment">
  <p>Assign a user to the following files:</p>
  <div class="resource-tree">
    <RootNode nodes={resourceNodes} />
  </div>
  <Form>
    <FormGroup>
      <TextInput
        bind:value={username}
        class="full-width"
        id={idElmntUsername}
        invalid={isUsernameInvalid}
        invalidText="A valid username must be entered."
        labelText="User name"
        placeholder="Enter user name..."
        required={true} />
    </FormGroup>
  </Form>
  <AssignmentErrors {assignmentErrors} />
</Modal>
