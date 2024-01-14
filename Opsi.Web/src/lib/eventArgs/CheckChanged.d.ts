import ResourceModel  from "../models/Resource"

export default interface CheckChanged {
    isChecked: boolean,
    resources: Array<ResourceModel>
}