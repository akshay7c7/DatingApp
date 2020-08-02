import { Component, OnInit, ViewChild, HostListener } from '@angular/core';
import { AuthService } from 'src/app/_services/auth.service';
import { ActivatedRoute } from '@angular/router';
import { User } from 'src/app/_models/user';
import { AlertifyService } from 'src/app/_services/alertify.service';
import { NgForm } from '@angular/forms';
import { UserService } from 'src/app/_services/user.service';

@Component({
  selector: 'app-member-edit',
  templateUrl: './member-edit.component.html',
  styleUrls: ['./member-edit.component.css']
})

export class MemberEditComponent implements OnInit {

  @ViewChild('editForm',{static:true}) editForm:NgForm ;
  @HostListener('window:beforeunload',['$event'])

  unloadNotification($event:any)
  {
    if(this.editForm.dirty)
    {
      $event.returnValue = true;
    }
  }

  user:User;
  photoUrl : string;

  constructor(
    private route : ActivatedRoute,
    private userService : UserService,
    private alertify : AlertifyService,
    private authService: AuthService) { }


  ngOnInit()
  {
    this.route.data.subscribe(
      data=>{
        this.user = data['editResolver'];
            })
      
    this.authService.currentPhotoUrl.subscribe(data => this.photoUrl= data)
  }


  updateUser()
  {
    this.userService.updateUser(this.authService.decodedToken.nameid, this.user)
    .subscribe
    (
      next=>{
          this.alertify.success('Profile updated successfully');
          this.editForm.reset(this.user);
            },

      error=>{
          this.alertify.error(error);
            }
    )
  }


  profileChangetrigger(url)
  {
    this.user.photoUrl = url;
  } 
}