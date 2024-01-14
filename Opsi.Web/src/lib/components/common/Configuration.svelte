<script lang="ts">
  import { fade } from "svelte/transition";
  import { Button, ButtonSet, Column, Form, FormGroup, Grid, NumberInput, Row, TextInput } from "carbon-components-svelte";
  import { DocumentExport, DocumentImport } from "carbon-icons-svelte";
  import Configuration from "../../models/configuration/Configuration";
  import { getConfig, setConfig } from "@/lib/services/configurationService";
  import { fetchCount } from "@/lib/stores/projectsStore";
  import { adminUsername, freelancerUsername } from "@/lib/stores/usersStore";
  
  const config = getConfig();

  async function exportConfig() {
    let fileHandle: FileSystemFileHandle;

    try {
      fileHandle = await window.showSaveFilePicker({
        startIn: "downloads",
        suggestedName: "config.opsi.json"
      });
    } catch (err: any) {
        if (err.message === "window.showSaveFilePicker is not a function") {
            alert("The file system API has not been enabled on this browser.\n\nIt may be protected behind an experimental flag.")
        } else {
            console.error(err);
        }
        return;
    }

    const writable = await fileHandle.createWritable();
    await writable.write(JSON.stringify(config, null, 2));
    await writable.close();
  }

  async function loadConfigFromFile() {
    
    let file: File;
    let fileHandles: FileSystemFileHandle[];

    try {
        do {
            fileHandles = await window.showOpenFilePicker({
                types: [{
                  accept: {
                      "application/json": [".json"]
                  },
                  description: "OPSI.Web configuration"
                }],
                excludeAcceptAllOption: true,
                multiple: false,
              });
            if (fileHandles.length !== 1) {
                alert("Select only file.");
            }
        } while (fileHandles.length !== 1)

        file = await fileHandles[0].getFile();
    } catch (err: any) {
        if (err.message === "window.showOpenFilePicker is not a function") {
            alert("The file system API has not been enabled on this browser.\n\nIt may be protected behind an experimental flag.")
        } else {
            console.error(err);
        }
        return;
    }


    try {
        let config = JSON.parse(await file.text()) as Configuration;
        setConfig(config);
    } catch (err) {
        console.error(err);
    }
  }

  $: {
    config.ui.projects.fetchCount = $fetchCount;
    config.users.administrator.username = $adminUsername;
    config.users.freelancer.username = $freelancerUsername;

    setConfig(config);
  }
</script>

<div in:fade="{{duration: 500}}">

  <Grid fullWidth noGutter>
    <Row>
      <Column>
        <h1>Configuration</h1>
      </Column>
    </Row>
    <Row>
      <Column>
        <p>The various properties may be configured here.</p>
      </Column>
    </Row>
    <Row>
      <Column sm={4} md={4} lg={6}>
        <h3>Users</h3>
        <p>Configure the usernames.</p>
        <Form>
          <FormGroup>
            <TextInput
              invalidText="An administrator username must be specified."
              labelText="Administrator"
              placeholder="Username of the administrator user"
              required={true}
              bind:value={$adminUsername} />
          </FormGroup>
          <FormGroup>
            <TextInput
              invalidText="A freelancer username must be specified."
              labelText="Freelancer"
              placeholder="Username of the freelancer user"
              required={true}
              bind:value={$freelancerUsername} />
          </FormGroup>
        </Form>
      </Column>
    </Row>
    <Row>
      <Column sm={4} md={4} lg={6}>
        <h3>User interface</h3>
        <p>These settings determine the appearance of the application.</p>
        <Form>
          <FormGroup>
            <NumberInput
              helperText="This determines the number of projects which are initially loaded, and then additionally fetched on each load."
              label="Project fetch count"
              max={50}
              min={1}
              bind:value={$fetchCount} />
          </FormGroup>
        </Form>
      </Column>
    </Row>
    <Row>
      <Column sm={4} md={4} lg={6}>
        <h3>Import / Export</h3>
        <p>A configuration may be imported, or the current configuration may be exported.</p>
        <ButtonSet>
          <Button
            icon={DocumentImport}
            size="small"
            on:click={loadConfigFromFile}>
            Select file
          </Button>
          <Button
            icon={DocumentExport}
            kind="tertiary"
            size="small"
            on:click={exportConfig}>
            Export
          </Button>
        </ButtonSet>
      </Column>
    </Row>
  </Grid>
</div>

<style lang="scss">
  $h3_margin_bottom: 32px;
  $h3_margin_top: $h3_margin_bottom * 2;

  h3 {
    margin: $h3_margin_top 0 $h3_margin_bottom;

    & + p {
        margin-bottom: $h3_margin_bottom;
        margin-top: -$h3_margin_bottom;
    }
  }
</style>
