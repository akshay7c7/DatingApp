import { Injectable } from "@angular/core";
import { CanDeactivate } from '@angular/router';
import { MemberEditComponent } from '../members/member-edit/member-edit.component';

@Injectable()
export class PrevetUnsavedChanges implements CanDeactivate<MemberEditComponent>{
    
    constructor() {
 
    }

    canDeactivate(component : MemberEditComponent){
        if(component.editForm.dirty)
        { 
            return confirm("Are you sure you want to proceed without saving?");
        }
        return true;
    }
}