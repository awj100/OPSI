<script lang="ts">
  import { FileUploaderButton, Modal } from "carbon-components-svelte";
  import InlineLoadingActive from "./wrappedCarbonComponents/inlineLoading/InlineLoadingActive.svelte";
  import InlineLoadingError from "./wrappedCarbonComponents/inlineLoading/InlineLoadingError.svelte";
  import InlineLoadingFinished from "./wrappedCarbonComponents/inlineLoading/InlineLoadingFinished.svelte";
  import { createEventDispatcher, type SvelteComponent } from 'svelte';
  import { Upload } from "carbon-icons-svelte";
  import ProjectDetailModel from "../../../models/ProjectDetail";
  import ResourceModel from "../../../models/Resource";
  import RootNode from "./treeView/RootNode.svelte";
  import type NodeModel from "./treeView/Node";
  import { upload } from "../../../services/resourcesService";
  import { errors } from "@/lib/stores/errorsStore";
  import { freelancerAuthToken } from "@/lib/stores/usersStore";
  import OpsiError from "@/lib/models/OpsiError";

  export let openModal: boolean = false;
  export let projectDetail: ProjectDetailModel;
  export let selectedTreeNodeId: string | undefined = "12345/de-CH/test-1.sdlxliff";

  let canUpload: boolean = false;
  const dispatch = createEventDispatcher();
  let isUploading: boolean = false;
  let selectedFileHasError: boolean = false;
  let selectedFiles: ReadonlyArray<File> = [];
  let uploaderButtonKind: "primary" | "danger" | "secondary" | "tertiary" | "ghost" | "danger-tertiary" | "danger-ghost" | undefined = "primary";

  let primaryButtonIcon: typeof SvelteComponent<any> = Upload;

  $: resourceNodes = getStructure(projectDetail.resources);
  $: selectedFileHasError = !!selectedTreeNodeId && selectedFiles.length === 1 && !selectedTreeNodeId.endsWith(selectedFiles[0].name);
  $: uploaderButtonKind = selectedFileHasError ? "danger" : "primary";
  $: canUpload = selectedFiles.length > 0 && !selectedFileHasError;

  function filesSelected(e: CustomEvent<ReadonlyArray<File>>) {
    // console.log(selectedFiles);
  }

  function getStructure(resources: ResourceModel[]): NodeModel[] {
    const result: Array<any> = [];
    const level = { result };
    const paths = resources.map((resource: ResourceModel) => resource.fullName!);
    const pathSeparator = "/";

    function reducer(accumulator: any, currentValue: any, idx: number, arr: Array<string>) {
      if (accumulator[currentValue]) {
          return accumulator[currentValue];
      }

      accumulator[currentValue] = {
          result: []
      };

      const node: NodeModel = {
          children: [],
          isFile: () => false,
          isSelected: arr.join(pathSeparator) == selectedTreeNodeId,
          text: currentValue
      };
      node.isFile = () => node.children.length === 0;

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

  function uploadFile() {
    isUploading = true;
    primaryButtonIcon = InlineLoadingActive;

    upload($freelancerAuthToken, projectDetail.id!, selectedTreeNodeId!, selectedFiles[0])
    .then(() => {
      primaryButtonIcon = InlineLoadingFinished;
      window.setTimeout(() => {
        dispatch("resourceUploaded", selectedTreeNodeId);
        openModal = false;
      }, 500);
    })
    .catch((error: Error) => {
      primaryButtonIcon = InlineLoadingError;
      errors.add(new OpsiError("Resource Upload Error",
                               `An error occurred while uploading "${selectedFiles[0].name}": ${error.message}`));
    });
  }
</script>

<Modal
  hasForm={true}
  hasScrollingContent={true}
  shouldSubmitOnEnter={false}
  bind:open={openModal}
  modalHeading="Resource Upload"
  primaryButtonDisabled={!canUpload}
  primaryButtonIcon={primaryButtonIcon}
  primaryButtonText="Upload"
  secondaryButtonText="Cancel"
  on:click:button--primary={uploadFile}
  on:click:button--secondary={() => (openModal = false)}
  on:open
  on:close>
  <FileUploaderButton
    bind:files={selectedFiles}
    disabled={isUploading}
    kind={uploaderButtonKind}
    labelText="Browse for resource"
    on:change={filesSelected} />
  <div class="resource-tree">
    <RootNode nodes={resourceNodes} />
  </div>
</Modal>
