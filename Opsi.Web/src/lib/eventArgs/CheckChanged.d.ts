import ResourceModel  from "../Models/Resource"

export default interface CheckChanged {
    isChecked: boolean,
    resources: Array<ResourceModel>
}